using FluentValidation;
using FluentValidation.Results;
using FocusTemplate.Admin.Shared;
using Microsoft.AspNetCore.Components.Forms;

namespace FocusTemplate.Admin.Web.Foundation.Validation.Core;

// The browser's messages show once their field is touched, the server's until it changes. One without a rendered input
// goes to the summary rather than nowhere
public sealed class FormMessages(IReadOnlyDictionary<string, string>? renames = null)
{
	private readonly List<PathMessage> clientMessages = [];

	private readonly List<FieldRegistration> fields = [];

	private readonly Dictionary<string, string> renames = new(
		renames ?? new Dictionary<string, string>(),
		StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<FieldIdentifier> touched = [];

	private List<PathMessage> serverMessages = [];

	private bool submitted;

	public IReadOnlyList<FieldMessage> Unmapped =>
	[
		.. this.serverMessages.Where(message => ServerField(message.Path) is null).Select(message => message.Message),
		.. this.clientMessages.Where(message => this.submitted && ClientField(message.Path) is null)
			.Select(message => message.Message),
	];

	public void ClearServerErrors() => this.serverMessages = [];

	public async Task FocusFirstInvalidAsync()
	{
		FieldRegistration? invalid = this.fields.Find(candidate =>
			candidate.Focus is not null && For(candidate.Field).Any(message => message.Severity == Severity.Error));
		if (invalid?.Focus is not null)
		{
			await invalid.Focus();
		}
	}

	public IReadOnlyList<FieldMessage> For(FieldIdentifier field)
	{
		bool visible = this.submitted || this.touched.Contains(field);
		IReadOnlyList<FieldMessage> fromClient =
		[
			.. this.clientMessages.Where(message => visible && Is(ClientField(message.Path), field))
				.Select(message => message.Message),
		];

		// A rule both ends run fails twice, once is enough
		return
		[
			.. fromClient,
			.. this.serverMessages.Where(message => Is(ServerField(message.Path), field))
				.Select(message => message.Message)
				.Where(message => message.Code is null || fromClient.All(shown => shown.Code != message.Code)),
		];
	}

	public FieldRegistration Register(string path, FieldIdentifier field, Func<Task>? focus = null)
	{
		FieldRegistration registration = new(this, path, field, focus);
		this.fields.Add(registration);
		return registration;
	}

	public void SetServerErrors(ValidationProblemDetails problem) =>
		this.serverMessages = problem.Violations.Count > 0
			? [.. problem.Violations.Select(violation => new PathMessage(violation.Key, MessageOf(violation))),]
			:
			[
				.. problem.Errors.SelectMany(error =>
					error.Value.Select(text => new PathMessage(
						error.Key,
						new FieldMessage(text, Severity.Error, null)))),
			];

	// At the field and beneath its path, so a change to a list drops the messages of its items, whose indexes no longer hold
	internal void DropServerMessages(FieldIdentifier field) =>
		this.serverMessages.RemoveAll(message => Is(ServerField(message.Path), field) || IsBeneathInputOf(field, message.Path));

	// Every field shows its messages from now on
	internal void MarkSubmitted() => this.submitted = true;

	// Null while no rendered input claims the field
	internal string? PathOf(FieldIdentifier field) =>
		this.fields.Find(registration => registration.Field.Equals(field))?.Path;

	// Replaces what its rules found before, at, beneath and above its paths, or in the whole form without paths
	internal void Replace(IReadOnlyCollection<string>? paths, ValidationResult result)
	{
		List<PathMessage> found =
		[
			.. result.Errors.Select(failure => new PathMessage(failure.PropertyName, MessageOf(failure))),
		];

		this.clientMessages.RemoveAll(message =>
			paths is null || paths.Any(path => FieldPath.Overlap(message.Path, path)) ||
			found.Exists(failure => failure.Path == message.Path));
		this.clientMessages.AddRange(found);
	}

	// False for a field touched before
	internal bool Touch(FieldIdentifier field) => this.touched.Add(field);

	internal void Unregister(FieldRegistration registration) => this.fields.Remove(registration);

	private static bool Is(FieldIdentifier? candidate, FieldIdentifier field) =>
		candidate is { } found && found.Equals(field);

	private static FieldMessage MessageOf(Violation violation) =>
		new(
			violation.Message,
			violation.Severity switch
			{
				ViolationSeverity.Warning => Severity.Warning,
				ViolationSeverity.Info => Severity.Info,
				_ => Severity.Error,
			},
			violation.Code);

	private static FieldMessage MessageOf(ValidationFailure failure) =>
		new(failure.ErrorMessage, failure.Severity, failure.ErrorCode);

	// The view model validator's failures carry the view model's own paths, exactly as its inputs spell them
	private FieldIdentifier? ClientField(string path) =>
		this.fields.Find(registration => string.Equals(registration.Path, path, StringComparison.Ordinal))?.Field;

	private bool IsBeneathInputOf(FieldIdentifier field, string path) =>
		this.fields.Exists(registration =>
			registration.Field.Equals(field) && FieldPath.IsBeneath(path, registration.Path));

	// Wire paths are camelCase, the view model's PascalCase. A rename leads to the view model's own path, matched exactly
	private FieldIdentifier? ServerField(string path) =>
		this.renames.TryGetValue(path, out string? renamed)
			? ClientField(renamed)
			: this.fields.Find(registration => string.Equals(
					registration.Path,
					path,
					StringComparison.OrdinalIgnoreCase))
				?.Field;

	private readonly record struct PathMessage(string Path, FieldMessage Message);
}
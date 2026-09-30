using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components.Forms;

namespace FocusTemplate.Admin.Web.Foundation.Validation.Core;

// A change validates its field and those depending on it, a submit the whole form; an answer a later run replaced is dropped
public sealed class FormValidation(
	FormMessages messages,
	ValidateScope validate,
	IReadOnlyDictionary<string, IReadOnlyList<string>>? dependencies = null) : IDisposable
{
	// Waiting for the server, null paths for the whole form; cancelling one drops its answer
	private readonly Dictionary<CancellationTokenSource, IReadOnlyCollection<string>?> pending = [];

	public void Dispose() => Cancel([.. this.pending.Keys,]);

	// False when a later run took its place
	public async Task<bool> FieldChangedAsync(FieldIdentifier field)
	{
		messages.Touch(field);
		messages.DropServerMessages(field);
		return await RunAsync(ScopeOf(field)) is not null;
	}

	// Leaving a field touches it, so an empty required field shows once the user moves on
	public async Task<bool> FieldFocusLostAsync(FieldIdentifier field) =>
		messages.Touch(field) && await RunAsync(ScopeOf(field)) is not null;

	// Only errors block, as on the server; null when a change or a second submit took its place, so the form is sent once
	public async Task<bool?> ValidateAllAsync()
	{
		messages.MarkSubmitted();
		return await RunAsync(null) is { } result
			? !result.Errors.Exists(failure => failure.Severity == Severity.Error)
			: null;
	}

	// Null stands for the whole form, which shares a field with every scope
	private static bool Overlap(IReadOnlyCollection<string>? scope, IReadOnlyCollection<string>? other) =>
		scope is null || other is null || scope.Any(path => other.Any(otherPath => FieldPath.Overlap(path, otherPath)));

	// Removed first: cancelling can resume a run on the spot, whose cleanup then finds nothing left to remove
	private void Cancel(IReadOnlyList<CancellationTokenSource> validationTokenSources)
	{
		foreach (CancellationTokenSource validationTokenSource in validationTokenSources)
		{
			this.pending.Remove(validationTokenSource);
		}

		foreach (CancellationTokenSource validationTokenSource in validationTokenSources)
		{
			validationTokenSource.Cancel();
		}
	}

	// Replaces every waiting run it shares a field with, since those read an older value
	private async Task<ValidationResult?> RunAsync(IReadOnlyCollection<string>? paths)
	{
		KeyValuePair<CancellationTokenSource, IReadOnlyCollection<string>?>[] replaced =
		[
			.. this.pending.Where(waiting => Overlap(waiting.Value, paths)),
		];

		Cancel([.. replaced.Select(waiting => waiting.Key),]);

		IReadOnlyCollection<string>? scope = paths is null || replaced.Any(waiting => waiting.Value is null)
			? null
			: [.. paths.Concat(replaced.SelectMany(waiting => waiting.Value ?? [])).Distinct(StringComparer.Ordinal),];

		using CancellationTokenSource validationTokenSource = new();
		this.pending[validationTokenSource] = scope;

		try
		{
			ValidationResult result = await validate(scope, validationTokenSource.Token);

			if (validationTokenSource.IsCancellationRequested)
			{
				return null;
			}

			messages.Replace(scope, result);
			return result;
		}
		catch (OperationCanceledException) when (validationTokenSource.IsCancellationRequested)
		{
			return null;
		}
		finally
		{
			this.pending.Remove(validationTokenSource);
		}
	}

	// A row's input stands for its list; a field no input claims may be read by any rule, so the whole form
	private IReadOnlyCollection<string>? ScopeOf(FieldIdentifier field)
	{
		if (messages.PathOf(field) is not { } registered)
		{
			return null;
		}

		string path = registered.Split('[')[0];
		return [path, .. dependencies?.GetValueOrDefault(path) ?? [],];
	}
}
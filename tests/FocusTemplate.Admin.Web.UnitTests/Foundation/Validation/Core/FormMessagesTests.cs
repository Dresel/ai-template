using System.Linq.Expressions;
using FluentValidation;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Web.Foundation.Validation.Core;
using Microsoft.AspNetCore.Components.Forms;
using static FocusTemplate.Admin.Web.UnitTests.Foundation.Validation.Core.FormTestSupport;

namespace FocusTemplate.Admin.Web.UnitTests.Foundation.Validation.Core;

// Where the server's messages show; the view model validator's are FormValidationTests'
public sealed class FormMessagesTests
{
	private readonly TestForm form = new();

	private readonly FormMessages messages = new();

	[Fact]
	public void AMessageFollowsItsInputOffScreenAndBack()
	{
		FieldRegistration street = this.messages.Register(
			"Address.Street",
			FieldIdentifier.Create(() => this.form.Address.Street));
		this.messages.SetServerErrors(Problem(("address.street", "maxLength", ViolationSeverity.Error)));

		street.Dispose();
		Assert.Equal(["maxLength",], Codes(this.messages.Unmapped));

		FieldIdentifier again = Input(() => this.form.Address.Street);
		Assert.Equal(["maxLength",], Codes(this.messages.For(again)));
		Assert.Empty(this.messages.Unmapped);
	}

	[Fact]
	public void APathNoInputClaimsGoesToTheSummary()
	{
		Input(() => this.form.Address.Street);

		this.messages.SetServerErrors(Problem(("address", "address.unknown", ViolationSeverity.Error)));

		Assert.Equal(["address.unknown",], Codes(this.messages.Unmapped));
	}

	[Fact]
	public void ARenameLeadsTheErrorOfAMemberTheViewModelNamesDifferently()
	{
		FormMessages renamed = new(new Dictionary<string, string> { ["name"] = nameof(TestForm.DisplayName), });
		FieldIdentifier displayName = Input(renamed, () => this.form.DisplayName);

		renamed.SetServerErrors(Problem(("name", "maxLength", ViolationSeverity.Error)));

		Assert.Equal(["maxLength",], Codes(renamed.For(displayName)));
		Assert.Empty(renamed.Unmapped);
	}

	[Fact]
	public void AnErrorsOnlyProblemShowsItsMessagesAsErrors()
	{
		FieldIdentifier name = Input(() => this.form.Name);
		Dictionary<string, IReadOnlyList<string>> errors = new() { ["name"] = ["Too long.",], };

		this.messages.SetServerErrors(new ValidationProblemDetails(errors, []));

		FieldMessage message = Assert.Single(this.messages.For(name));
		Assert.Equal(("Too long.", Severity.Error, (string?)null), (message.Text, message.Severity, message.Code));
	}

	[Fact]
	public void ServerPathsFindTheirInputsIgnoringCase()
	{
		TestTag second = new();
		this.form.Tags.AddRange([new TestTag(), second,]);
		FieldIdentifier street = Input(() => this.form.Address.Street);
		FieldIdentifier tag = Input(() => second.Value, "Tags[1]");

		this.messages.SetServerErrors(
			Problem(
				("address.street", "maxLength", ViolationSeverity.Error),
				("tags[1]", "minLength", ViolationSeverity.Error)));

		Assert.Equal(["maxLength",], Codes(this.messages.For(street)));
		Assert.Equal(["minLength",], Codes(this.messages.For(tag)));
		Assert.Empty(this.messages.Unmapped);
	}

	private static FieldIdentifier Input<TValue>(
		FormMessages store,
		Expression<Func<TValue>> value,
		string? path = null)
	{
		FieldIdentifier field = FieldIdentifier.Create(value);
		store.Register(path ?? FieldPath.Of(value), field);
		return field;
	}

	private FieldIdentifier Input<TValue>(Expression<Func<TValue>> value, string? path = null) =>
		Input(this.messages, value, path);
}
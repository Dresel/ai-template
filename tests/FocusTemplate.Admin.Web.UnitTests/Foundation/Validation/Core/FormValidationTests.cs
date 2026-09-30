using System.Linq.Expressions;
using FluentValidation;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Web.Foundation.Validation.Core;
using Microsoft.AspNetCore.Components.Forms;
using static FocusTemplate.Admin.Web.UnitTests.Foundation.Validation.Core.FormTestSupport;

namespace FocusTemplate.Admin.Web.UnitTests.Foundation.Validation.Core;

// When the view model validator runs and over which fields, and what its runs leave in the messages
public sealed class FormValidationTests : IDisposable
{
	private readonly TestForm form = new();

	private readonly FormMessages messages;

	// The paths each run validated, null for the whole form
	private readonly List<string[]?> validated = [];

	private readonly FormValidation validation;

	public FormValidationTests()
	{
		FormUnderTest main = Form(new TestFormValidator());
		(this.messages, this.validation) = (main.Messages, main.Validation);
	}

	[Fact]
	public async Task AChangeValidatesOnlyItsFieldAndTheFieldsDependingOnIt()
	{
		FieldIdentifier min = Input(() => this.form.Min);
		FieldIdentifier name = Input(() => this.form.Name);
		Input(() => this.form.Max);

		await this.validation.FieldChangedAsync(min);
		await this.validation.FieldChangedAsync(name);

		string[][] expected = [["Min", "Max",], ["Name",],];
		Assert.Equal(expected, this.validated);
	}

	[Fact]
	public async Task AChangeValidatesTheFieldsDependingOnIt()
	{
		FieldIdentifier min = Input(() => this.form.Min);
		FieldIdentifier max = Input(() => this.form.Max);

		this.form.Max = 10;
		await this.validation.FieldChangedAsync(max);
		Assert.Empty(this.messages.For(max));

		this.form.Min = 30;
		await this.validation.FieldChangedAsync(min);
		Assert.Equal(["belowMin",], Codes(this.messages.For(max)));
	}

	// Its answer is for the values before the change, and the change comes first
	[Fact]
	public async Task AChangeWhileASubmitWaitsDropsTheSubmit()
	{
		TaskCompletionSource<bool> answer = new();
		using FormUnderTest checking = Form(new TestFormValidator((_, _) => answer.Task));
		FieldIdentifier name = Input(checking.Messages, () => this.form.Name);
		Input(checking.Messages, () => this.form.DisplayName);
		(this.form.Name, this.form.DisplayName) = ("Bob", "Ann");

		Task<bool?> submitted = checking.Validation.ValidateAllAsync();
		this.form.Name = "Alexander";
		Task<bool> changed = checking.Validation.FieldChangedAsync(name);
		answer.SetResult(false);

		Assert.Null(await submitted);
		Assert.True(await changed);
		Assert.Equal(["maxLength",], Codes(checking.Messages.For(name)));
	}

	// The run over all fields started before the change and holds the field's older value
	[Fact]
	public async Task AChangeWhileAllFieldsAreValidatedLeavesNoOlderMessage()
	{
		TaskCompletionSource<bool> answer = new();
		using FormUnderTest checking = Form(new TestFormValidator((_, _) => answer.Task));
		FieldIdentifier name = Input(checking.Messages, () => this.form.Name);
		Input(checking.Messages, () => this.form.DisplayName);
		(this.form.Name, this.form.DisplayName) = ("Alexander", "Ann");

		Task<bool> all = checking.Validation.FieldChangedAsync(FieldIdentifier.Create(() => this.form.Nickname));
		this.form.Name = "Alex";
		Task<bool> changed = checking.Validation.FieldChangedAsync(name);
		answer.SetResult(false);
		await Task.WhenAll(all, changed);

		Assert.Empty(checking.Messages.For(name));
		Assert.False(await all);
	}

	// The display name's run validates the nickname too, and read its older value before it waited for the server
	[Fact]
	public async Task AChangeWhileAnOverlappingRunWaitsLeavesNoOlderMessage()
	{
		TaskCompletionSource<bool> answer = new();
		using FormUnderTest checking = Form(new TestFormValidator((_, _) => answer.Task));
		FieldIdentifier displayName = Input(checking.Messages, () => this.form.DisplayName);
		FieldIdentifier nickname = Input(checking.Messages, () => this.form.Nickname);
		(this.form.DisplayName, this.form.Nickname) = ("Ann", "Nicknamed");

		Task<bool> overlapping = checking.Validation.FieldChangedAsync(displayName);
		this.form.Nickname = "Nick";
		Task<bool> changed = checking.Validation.FieldChangedAsync(nickname);
		answer.SetResult(true);
		await Task.WhenAll(overlapping, changed);

		Assert.Empty(checking.Messages.For(nickname));
		Assert.Equal(["taken",], Codes(checking.Messages.For(displayName)));
	}

	[Fact]
	public async Task AClientFailureNoInputClaimsGoesToTheSummaryOnSubmit()
	{
		this.form.Name = "Alexander";

		Assert.False(await this.validation.ValidateAllAsync());

		Assert.Equal(["maxLength",], Codes(this.messages.Unmapped));
	}

	[Fact]
	public async Task AClientMessageWaitsUntilItsFieldIsTouched()
	{
		FieldIdentifier name = Input(() => this.form.Name);
		this.form.Name = "Alexander";

		// A field no input claims validates the whole form, Name included
		await this.validation.FieldChangedAsync(FieldIdentifier.Create(() => this.form.DisplayName));
		Assert.Empty(this.messages.For(name));

		await this.validation.FieldChangedAsync(name);
		Assert.Equal(["maxLength",], Codes(this.messages.For(name)));
	}

	[Fact]
	public async Task AFailureBothEndsReportShowsOnce()
	{
		FieldIdentifier nickname = Input(() => this.form.Nickname);
		this.form.Nickname = "BOB";
		await this.validation.FieldChangedAsync(nickname);

		this.messages.SetServerErrors(Problem(("nickname", "shouting", ViolationSeverity.Warning)));

		Assert.Equal(["shouting",], Codes(this.messages.For(nickname)));
	}

	// Which of them a field shows is the inputs' decision
	[Fact]
	public async Task AFieldKeepsItsErrorsWarningsAndInfosTogether()
	{
		FieldIdentifier nickname = Input(() => this.form.Nickname);

		this.form.Nickname = "ADALOVELACE";
		await this.validation.FieldChangedAsync(nickname);
		Assert.Equal(
			["famous", "maxLength", "shouting",],
			Codes(this.messages.For(nickname)).Order(StringComparer.Ordinal));

		this.form.Nickname = "ADA";
		await this.validation.FieldChangedAsync(nickname);
		Assert.Equal(["famous", "shouting",], Codes(this.messages.For(nickname)).Order(StringComparer.Ordinal));
	}

	[Fact]
	public async Task AFieldNoInputClaimsValidatesTheWholeForm()
	{
		await this.validation.FieldChangedAsync(FieldIdentifier.Create(() => this.form.Nickname));

		string[]?[] expected = [null,];
		Assert.Equal(expected, this.validated);
	}

	[Fact]
	public async Task AFormGoneDropsTheAnswerItWaitedFor()
	{
		TaskCompletionSource<bool> answer = new();
		FormUnderTest checking = Form(new TestFormValidator((_, _) => answer.Task));
		FieldIdentifier displayName = Input(checking.Messages, () => this.form.DisplayName);
		this.form.DisplayName = "Ann";
		Task<bool> changed = checking.Validation.FieldChangedAsync(displayName);

		checking.Dispose();
		answer.SetResult(true);

		Assert.False(await changed);
		Assert.Empty(checking.Messages.For(displayName));
	}

	[Fact]
	public async Task AListRowsInputMovesToTheIndexItsRowHasNow()
	{
		TestTag first = new() { Value = "blue", };
		TestTag second = new() { Value = string.Empty, };
		this.form.Tags.AddRange([first, second,]);
		FieldRegistration firstInput = this.messages.Register("Tags[0]", FieldIdentifier.Create(() => first.Value));
		FieldRegistration secondInput = this.messages.Register("Tags[1]", FieldIdentifier.Create(() => second.Value));

		this.form.Tags.Remove(first);
		firstInput.Dispose();
		secondInput.Path = "Tags[0]";

		Assert.False(await this.validation.ValidateAllAsync());
		Assert.Equal(["minLength",], Codes(this.messages.For(secondInput.Field)));
	}

	[Fact]
	public async Task AListRowsInputValidatesItsWholeList()
	{
		TestTag second = new();
		this.form.Tags.AddRange([new TestTag(), second,]);
		FieldIdentifier tag = Input(() => second.Value, "Tags[1]");

		await this.validation.FieldChangedAsync(tag);

		string[][] expected = [["Tags",],];
		Assert.Equal(expected, this.validated);
	}

	[Fact]
	public async Task ARuleAskingTheServerShowsOnceTheAnswerIsIn()
	{
		TaskCompletionSource<bool> answer = new();
		using FormUnderTest checking = Form(new TestFormValidator((_, _) => answer.Task));
		FieldIdentifier displayName = Input(checking.Messages, () => this.form.DisplayName);
		this.form.DisplayName = "Ann";

		Task<bool> changed = checking.Validation.FieldChangedAsync(displayName);
		Assert.Empty(checking.Messages.For(displayName));

		answer.SetResult(true);
		Assert.True(await changed);
		Assert.Equal(["taken",], Codes(checking.Messages.For(displayName)));
	}

	[Fact]
	public void ARuleNeedingNoServerShowsAtOnce()
	{
		FieldIdentifier name = Input(() => this.form.Name);
		this.form.Name = "Alexander";

		Task<bool> changed = this.validation.FieldChangedAsync(name);

		Assert.True(changed.IsCompletedSuccessfully);
		Assert.Equal(["maxLength",], Codes(this.messages.For(name)));
	}

	// So a second click on submit while the first waits for the server sends the form once
	[Fact]
	public async Task ASecondSubmitTakesThePlaceOfTheFirst()
	{
		TaskCompletionSource<bool> answer = new();
		using FormUnderTest checking = Form(new TestFormValidator((_, _) => answer.Task));
		Input(checking.Messages, () => this.form.DisplayName);
		(this.form.Name, this.form.DisplayName) = ("Bob", "Ann");

		Task<bool?> first = checking.Validation.ValidateAllAsync();
		Task<bool?> second = checking.Validation.ValidateAllAsync();
		answer.SetResult(false);

		Assert.Null(await first);
		Assert.True(await second);
	}

	[Fact]
	public async Task AnAnswerForAnOlderValueIsDropped()
	{
		TaskCompletionSource<bool> older = new();
		TaskCompletionSource<bool> newer = new();
		using FormUnderTest checking =
			Form(new TestFormValidator((name, _) => name == "Ann" ? older.Task : newer.Task));
		FieldIdentifier displayName = Input(checking.Messages, () => this.form.DisplayName);

		this.form.DisplayName = "Ann";
		Task<bool> first = checking.Validation.FieldChangedAsync(displayName);
		this.form.DisplayName = "Bob";
		Task<bool> second = checking.Validation.FieldChangedAsync(displayName);
		newer.SetResult(false);
		older.SetResult(true);

		Assert.True(await second);
		Assert.False(await first);
		Assert.Empty(checking.Messages.For(displayName));
	}

	[Fact]
	public async Task AnItemsMessageGoesOnceItsListIsValidatedAgain()
	{
		TestTag first = new() { Value = string.Empty, };
		TestTag second = new() { Value = "blue", };
		this.form.Tags.AddRange([first, second,]);
		FieldIdentifier firstInput = Input(() => first.Value, "Tags[0]");
		FieldIdentifier secondInput = Input(() => second.Value, "Tags[1]");
		await this.validation.FieldChangedAsync(firstInput);
		Assert.Equal(["minLength",], Codes(this.messages.For(firstInput)));

		first.Value = "red";
		await this.validation.FieldChangedAsync(secondInput);

		Assert.Empty(this.messages.For(firstInput));
	}

	[Fact]
	public async Task ChangingAListDropsTheServerMessagesOfItsItems()
	{
		TestTag first = new() { Value = "blue", };
		this.form.Tags.Add(first);
		FieldIdentifier tags = Input(() => this.form.Tags);
		FieldIdentifier tag = Input(() => first.Value, "Tags[0]");
		this.messages.SetServerErrors(
			Problem(("tags", "maxItems", ViolationSeverity.Error), ("tags[0]", "tag.banned", ViolationSeverity.Error)));

		this.form.Tags.Add(new TestTag { Value = "green", });
		await this.validation.FieldChangedAsync(tags);

		Assert.Empty(this.messages.For(tags));
		Assert.Empty(this.messages.For(tag));
	}

	public void Dispose() => this.validation.Dispose();

	[Fact]
	public async Task EditingAFieldDropsItsServerMessagesOnly()
	{
		FieldIdentifier name = Input(() => this.form.Name);
		FieldIdentifier nickname = Input(() => this.form.Nickname);
		(this.form.Name, this.form.Nickname) = ("Bob", "Bobby");
		this.messages.SetServerErrors(
			Problem(
				("name", "name.taken", ViolationSeverity.Error),
				("nickname", "nickname.taken", ViolationSeverity.Error)));

		this.form.Name = "Ann";
		await this.validation.FieldChangedAsync(name);

		Assert.Empty(this.messages.For(name));
		Assert.Equal(["nickname.taken",], Codes(this.messages.For(nickname)));
	}

	[Fact]
	public async Task FixingAValueClearsItsMessage()
	{
		FieldIdentifier name = Input(() => this.form.Name);

		this.form.Name = "Alexander";
		await this.validation.FieldChangedAsync(name);
		this.form.Name = "Alex";
		await this.validation.FieldChangedAsync(name);

		Assert.Empty(this.messages.For(name));
	}

	[Fact]
	public async Task FocusGoesToTheFirstInvalidInputInRenderOrder()
	{
		List<string> focused = [];
		this.messages.Register("Name", FieldIdentifier.Create(() => this.form.Name), Focus("name", focused));
		this.messages.Register(
			"Nickname",
			FieldIdentifier.Create(() => this.form.Nickname),
			Focus("nickname", focused));
		this.messages.Register(
			"Address.Street",
			FieldIdentifier.Create(() => this.form.Address.Street),
			Focus("street", focused));
		(this.form.Name, this.form.Nickname, this.form.Address.Street) = ("Bob", "Bobby Tables", "Long Street");

		await this.validation.ValidateAllAsync();
		await this.messages.FocusFirstInvalidAsync();

		Assert.Equal(["nickname",], focused);
	}

	[Fact]
	public async Task LeavingAFieldCountsAsTouchingIt()
	{
		FieldIdentifier name = Input(() => this.form.Name);
		FieldIdentifier nickname = Input(() => this.form.Nickname);

		Assert.True(await this.validation.FieldFocusLostAsync(name));

		Assert.Equal(["required",], Codes(this.messages.For(name)));
		Assert.Empty(this.messages.For(nickname));
	}

	[Fact]
	public async Task LeavingAFieldKeepsItsServerMessages()
	{
		FieldIdentifier name = Input(() => this.form.Name);
		this.form.Name = "Bob";
		this.messages.SetServerErrors(Problem(("name", "name.taken", ViolationSeverity.Error)));

		await this.validation.FieldFocusLostAsync(name);

		Assert.Equal(["name.taken",], Codes(this.messages.For(name)));
	}

	[Fact]
	public async Task LeavingATouchedFieldAgainChangesNothing()
	{
		FieldIdentifier name = Input(() => this.form.Name);
		this.form.Name = "Alexander";
		await this.validation.FieldChangedAsync(name);

		Assert.False(await this.validation.FieldFocusLostAsync(name));
	}

	[Fact]
	public async Task SubmittingTakesThePlaceOfTheChecksStillWaiting()
	{
		TaskCompletionSource<bool> answer = new();
		using FormUnderTest checking = Form(new TestFormValidator((_, _) => answer.Task));
		FieldIdentifier displayName = Input(checking.Messages, () => this.form.DisplayName);
		(this.form.Name, this.form.DisplayName) = ("Bob", "Ann");
		Task<bool> changed = checking.Validation.FieldChangedAsync(displayName);

		Task<bool?> submitted = checking.Validation.ValidateAllAsync();
		answer.SetResult(true);

		Assert.False(await submitted);
		Assert.False(await changed);
		Assert.Equal(["taken",], Codes(checking.Messages.For(displayName)));
	}

	[Fact]
	public async Task SubmittingTouchesEveryFieldAndFailsOnlyOnErrors()
	{
		FieldIdentifier name = Input(() => this.form.Name);
		FieldIdentifier nickname = Input(() => this.form.Nickname);
		this.form.Nickname = "BOB";

		Assert.False(await this.validation.ValidateAllAsync());
		Assert.Equal(["required",], Codes(this.messages.For(name)));
		Assert.Equal(["shouting",], Codes(this.messages.For(nickname)));

		this.form.Name = "Bob";
		Assert.True(await this.validation.ValidateAllAsync());
	}

	[Fact]
	public async Task SubmittingWaitsForTheAsyncRules()
	{
		TaskCompletionSource<bool> answer = new();
		using FormUnderTest checking = Form(new TestFormValidator((_, _) => answer.Task));
		FieldIdentifier displayName = Input(checking.Messages, () => this.form.DisplayName);
		(this.form.Name, this.form.DisplayName) = ("Bob", "Ann");

		Task<bool?> submitted = checking.Validation.ValidateAllAsync();
		Assert.False(submitted.IsCompleted);
		answer.SetResult(true);

		Assert.False(await submitted);
		Assert.Equal(["taken",], Codes(checking.Messages.For(displayName)));
	}

	private static Func<Task> Focus(string input, List<string> focused) =>
		() =>
		{
			focused.Add(input);
			return Task.CompletedTask;
		};

	private static FieldIdentifier Input<TValue>(
		FormMessages store,
		Expression<Func<TValue>> value,
		string? path = null)
	{
		FieldIdentifier field = FieldIdentifier.Create(value);
		store.Register(path ?? FieldPath.Of(value), field);
		return field;
	}

	// As FormHostBase builds them, recording what each run validates
	private FormUnderTest Form(TestFormValidator validator)
	{
		FormMessages store = new();
		FormValidation runs = new(
			store,
			(paths, cancellationToken) =>
			{
				this.validated.Add(paths?.ToArray());
				return paths is null
					? validator.ValidateAsync(this.form, cancellationToken)
					: validator.ValidateAsync(
						this.form,
						options => options.IncludeProperties([.. paths,]),
						cancellationToken);
			},
			validator.Dependencies);
		return new FormUnderTest(store, runs);
	}

	private FieldIdentifier Input<TValue>(Expression<Func<TValue>> value, string? path = null) =>
		Input(this.messages, value, path);

	private sealed record FormUnderTest(FormMessages Messages, FormValidation Validation) : IDisposable
	{
		public void Dispose() => Validation.Dispose();
	}
}
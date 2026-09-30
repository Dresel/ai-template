namespace FocusTemplate.Admin.Web.E2E;

// Only errors block: warnings and infos show without stopping the submit
[Collection(AspireCollection.Name)]
public sealed class ValidationSeverityTests(BlazorAppFixture app) : ValidationDemoTest(app)
{
	[Fact]
	public async Task AFieldShowsItsMostSevereMessageAndCountsTheRest()
	{
		await OpenAsync();

		await Input("nickname").FillAsync("SHOUTINGSHOUTINGSHOUTING");
		await Expect(Error("nickname")).ToBeVisibleAsync();
		await Expect(Warning("nickname")).ToBeHiddenAsync();
		await Expect(More("nickname")).ToHaveTextAsync("+1");

		await Input("nickname").FillAsync("SHOUTING");
		await Expect(Error("nickname")).ToBeHiddenAsync();
		await Expect(Warning("nickname")).ToBeVisibleAsync();
		await Expect(More("nickname")).ToBeHiddenAsync();

		await Input("age").FillAsync("121");
		await Expect(Error("age")).ToBeVisibleAsync();
		await Expect(Info("age")).ToBeHiddenAsync();
		await Expect(More("age")).ToHaveTextAsync("+1");
	}

	[Fact]
	public async Task AFieldWithSeveralErrorsCountsThemAllAndGroupsThemBySeverity()
	{
		await OpenAsync();

		await Input("nickname").FillAsync("SHOUTING 12 SHOUTING 34");
		await Expect(Error("nickname")).ToBeVisibleAsync();
		await Expect(More("nickname")).ToHaveTextAsync("+4");

		await More("nickname").ClickAsync();
		await Expect(MessagesDialog.GetByTestId("message-group-error").Locator("li")).ToHaveCountAsync(3);
		await Expect(MessagesDialog.GetByTestId("message-group-error"))
			.ToContainTextAsync("The nickname must not contain digits.");
		await Expect(MessagesDialog.GetByTestId("message-group-warning"))
			.ToContainTextAsync("The nickname is all upper case.");
		await Expect(MessagesDialog.GetByTestId("message-group-info"))
			.ToContainTextAsync("Letters only keep a nickname easy to type.");
	}

	[Fact]
	public async Task AWarningShowsAsAWarningAndLetsTheProfileThrough()
	{
		await OpenAsync();
		await FillValidAsync();

		await Input("nickname").FillAsync("SHOUTING");

		await Expect(Warning("nickname")).ToHaveTextAsync("The nickname is all upper case.");
		await Expect(Warning("nickname")).ToHaveAttributeAsync("role", "status");
		await Expect(Error("nickname")).ToBeHiddenAsync();
		await Expect(More("nickname")).ToBeHiddenAsync();
		await SubmitAsync();
		await Expect(Accepted).ToBeVisibleAsync();
	}

	[Fact]
	public async Task AnInfoShowsAsHelpTextAndLetsTheProfileThrough()
	{
		await OpenAsync();
		await FillValidAsync();

		await Input("age").FillAsync("101");

		await Expect(Info("age")).ToHaveTextAsync("Over a hundred: please check the age.");
		await Expect(Input("age")).Not.ToHaveAttributeAsync("aria-invalid", "true");
		await SubmitAsync();
		await Expect(Accepted).ToBeVisibleAsync();
	}

	[Fact]
	public async Task TheRestShowOnHoverAndInADialogGroupedBySeverity()
	{
		await OpenAsync();
		await Input("age").FillAsync("121");

		await Error("age").HoverAsync();
		await Expect(Page.GetByTestId("age-tooltip")).ToContainTextAsync("Over a hundred: please check the age.");

		await More("age").ClickAsync();
		await Expect(MessagesDialog.GetByTestId("message-group-error")).ToBeVisibleAsync();
		await Expect(MessagesDialog.GetByTestId("message-group-info"))
			.ToContainTextAsync("Over a hundred: please check the age.");
		await Expect(MessagesDialog.GetByTestId("message-group-warning")).ToBeHiddenAsync();
	}

	// The filter lets a request with warnings alone through, and the success response has no place for them
	[Fact]
	public async Task TheServersWarningsArriveOnlyWithAnError()
	{
		await OpenAsync();
		await CheckOnlyOnTheServerAsync();
		await FillValidAsync();
		await Input("nickname").FillAsync("SHOUTING");
		await Input("name").FillAsync(TooLongName);

		await SubmitAsync();
		await Expect(Error("name")).ToBeVisibleAsync();
		await Expect(Warning("nickname")).ToHaveTextAsync("The nickname is all upper case.");

		await Input("name").FillAsync("Ada");
		await SubmitAsync();
		await Expect(Accepted).ToBeVisibleAsync();
		await Expect(Warning("nickname")).ToBeHiddenAsync();
	}
}
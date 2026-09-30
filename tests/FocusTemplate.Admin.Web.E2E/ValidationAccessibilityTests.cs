namespace FocusTemplate.Admin.Web.E2E;

[Collection(AspireCollection.Name)]
public sealed class ValidationAccessibilityTests(BlazorAppFixture app) : ValidationDemoTest(app)
{
	[Fact]
	public async Task AServerErrorMarksItsInputAndTakesTheFocusThere()
	{
		await OpenAsync();
		await FillValidAsync();
		await Input("code").FillAsync("TAK");

		await SubmitAsync();

		await Expect(Error("code")).ToHaveAttributeAsync("role", "alert");
		await Expect(Input("code")).ToHaveAttributeAsync("aria-invalid", "true");
		await Expect(Input("code")).ToBeFocusedAsync();
	}

	[Fact]
	public async Task AMessageSurvivesItsInputBeingHiddenAndShownAgain()
	{
		await OpenAsync();
		await Input("nickname").FillAsync("SHOUTINGSHOUTINGSHOUTING");
		await Expect(Error("nickname")).ToBeVisibleAsync();

		await SwitchAsync("show-optional", false);
		await Expect(Input("nickname")).ToBeHiddenAsync();
		await SwitchAsync("show-optional", true);

		await Expect(Error("nickname")).ToBeVisibleAsync();
	}

	[Fact]
	public async Task AFieldsOtherMessagesAreAKeyPressAway()
	{
		await OpenAsync();
		await Input("nickname").FillAsync("SHOUTINGSHOUTINGSHOUTING");

		await More("nickname").FocusAsync();
		await Page.Keyboard.PressAsync("Enter");

		await Expect(MessagesDialog.GetByTestId("message-group-warning"))
			.ToContainTextAsync("The nickname is all upper case.");
	}

	[Fact]
	public async Task AMessageStaysWithItsRowWhenARowAboveGoes()
	{
		await OpenAsync();
		await AddTagsAsync("a", "b", "c");
		await Input("tag-1").FillAsync(string.Empty);
		await Expect(Error("tag-1")).ToBeVisibleAsync();

		await Page.GetByTestId("remove-tag-0").ClickAsync();

		await Expect(Error("tag-0")).ToBeVisibleAsync();
		await Expect(Error("tag-1")).ToBeHiddenAsync();
		await Expect(Input("tag-1")).ToHaveValueAsync("c");
	}
}
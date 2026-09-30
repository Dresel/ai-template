using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

// The 400 mapped onto the inputs: the server's own rules, and with the browser's checks off everything the server finds
[Collection(AspireCollection.Name)]
public sealed class ValidationFromTheServerTests(BlazorAppFixture app) : ValidationDemoTest(app)
{
	[Fact]
	public async Task AHandlersOwnProblemLandsAtTheInputOfTheRenamedMember()
	{
		await OpenAsync();
		await FillValidAsync();
		await Input("name").FillAsync("Reserved");

		await SubmitAsync();

		await Expect(Error("name")).ToHaveTextAsync("The name is reserved.");
	}

	[Fact]
	public async Task APathNoInputClaimsGoesToTheSummary()
	{
		await OpenAsync();
		await FillValidAsync();
		await Input("city").FillAsync("Atlantis");

		await SubmitAsync();

		await Expect(Summary.GetByTestId("message-group-error")).ToContainTextAsync("There is no such address.");
		await Expect(Error("city")).ToBeHiddenAsync();
	}

	[Fact]
	public async Task ARuleBothEndsRunShowsOnce()
	{
		await OpenAsync();
		await FillValidAsync();
		await Input("nickname").FillAsync("SHOUTING");

		// A rule only the server runs, so the request goes out
		await Input("city").FillAsync("Atlantis");
		await Expect(Warning("nickname")).ToBeVisibleAsync();

		await SubmitAsync();

		await Expect(Summary).ToBeVisibleAsync();
		await Expect(Warning("nickname")).ToHaveTextAsync("The nickname is all upper case.");
	}

	[Fact]
	public async Task ARuleOnlyTheServerKnowsShowsAtItsInput()
	{
		await OpenAsync();

		// With the browser's checks on, the code check catches TAK before the request
		await CheckOnlyOnTheServerAsync();
		await FillValidAsync();
		await Input("code").FillAsync("TAK");

		await SubmitAsync();

		await Expect(Error("code")).ToHaveTextAsync("The code is taken.");
	}

	[Theory]
	[InlineData(409)]
	[InlineData(500)]
	public async Task AnErrorThatIsNotAboutValidationShowsAsAnAlert(int status)
	{
		await Page.RouteAsync(
			"**/demo-profiles",
			route => route.FulfillAsync(
				new RouteFulfillOptions
				{
					Status = status,
					ContentType = "application/problem+json",
					Body = $$"""{ "title": "Something went wrong.", "status": {{status}} }""",
				}));
		await OpenAsync();
		await FillValidAsync();

		await SubmitAsync();

		await Expect(Failure).ToBeVisibleAsync();
		await Expect(Page.Locator("[aria-invalid='true']")).ToHaveCountAsync(0);
		await Expect(Summary).ToBeHiddenAsync();
	}

	[Fact]
	public async Task EditingAFieldDropsItsServerMessageAndLeavesTheOthers()
	{
		await OpenAsync();
		await CheckOnlyOnTheServerAsync();
		await FillValidAsync();
		await Input("code").FillAsync("TAK");
		await Input("name").FillAsync(TooLongName);
		await SubmitAsync();
		await Expect(Error("code")).ToBeVisibleAsync();

		await Input("code").FillAsync("ABC");

		await Expect(Error("code")).ToBeHiddenAsync();
		await Expect(Error("name")).ToBeVisibleAsync();
	}

	[Fact]
	public async Task NestedAndListPathsFromTheServerFindTheirInputs()
	{
		await OpenAsync();
		await CheckOnlyOnTheServerAsync();
		await FillValidAsync();
		await Input("street").FillAsync(new string('s', 101));
		await AddTagsAsync("ok", new string('t', 21));

		await SubmitAsync();

		await Expect(Error("street")).ToBeVisibleAsync();
		await Expect(Error("tag-1")).ToBeVisibleAsync();
		await Expect(Error("tag-0")).ToBeHiddenAsync();
	}

	[Fact]
	public async Task TheServersTemperatureRulesLandAtBoth()
	{
		await OpenAsync();
		await CheckOnlyOnTheServerAsync();
		await FillValidAsync();
		await Input("min-temperature").FillAsync("30");
		await Input("max-temperature").FillAsync("10");

		await SubmitAsync();

		await Expect(Error("max-temperature")).ToHaveTextAsync("The highest temperature must not be below the lowest.");
		await Expect(Error("min-temperature")).ToHaveTextAsync("The lowest temperature must not be above the highest.");
	}

	[Fact]
	public async Task WithoutTheBrowsersChecksTheServerReportsTheSameRulesAtTheSameInputs()
	{
		await OpenAsync();
		await CheckOnlyOnTheServerAsync();
		await FillValidAsync();
		await Input("code").FillAsync("abc");
		await Input("name").FillAsync(TooLongName);
		await Input("age").FillAsync("17");
		await Input("postal-code").FillAsync("119");
		foreach (string input in (string[])["code", "name", "age", "postal-code",])
		{
			await Expect(Error(input)).ToBeHiddenAsync();
		}

		await SubmitAsync();

		await Expect(Error("code")).ToHaveTextAsync("Three upper-case letters.");
		await Expect(Error("postal-code")).ToHaveTextAsync("Four digits.");
		await Expect(Error("name")).ToBeVisibleAsync();
		await Expect(Error("age")).ToBeVisibleAsync();
	}
}
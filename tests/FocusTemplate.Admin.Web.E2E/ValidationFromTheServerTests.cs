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

	[Fact]
	public async Task AnErrorThatIsNotAboutValidationShowsAsAnAlert()
	{
		await Page.RouteAsync(
			"**/demo-profiles",
			route => route.FulfillAsync(
				new RouteFulfillOptions
				{
					Status = 409,
					ContentType = "application/problem+json",
					Body = """{ "title": "Something went wrong.", "status": 409 }""",
				}));
		await OpenAsync();
		await FillValidAsync();

		await SubmitAsync();

		await Expect(Failure).ToBeVisibleAsync();
		await Expect(Page.Locator("[aria-invalid='true']")).ToHaveCountAsync(0);
		await Expect(Summary).ToBeHiddenAsync();
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
}
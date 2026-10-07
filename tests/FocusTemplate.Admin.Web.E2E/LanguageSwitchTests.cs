using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

// The test browser speaks English, so whatever arrives in German follows the choice in the app
[Collection(AspireCollection.Name)]
public sealed class LanguageSwitchTests(BlazorAppFixture app) : ValidationDemoTest(app)
{
	[Fact]
	public async Task TheChoiceHoldsAcrossLoads()
	{
		await Page.GotoAsync(App.BaseUrl);
		await Expect(Heading("Welcome")).ToBeVisibleAsync();

		await ChooseGermanAsync();
		await Page.ReloadAsync();

		await Expect(Heading("Willkommen")).ToBeVisibleAsync();
		await Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "de");
	}

	[Fact]
	public async Task TheLoadingScreenFollowsTheChoice()
	{
		await Page.GotoAsync(App.BaseUrl);
		await ChooseGermanAsync();

		// Without Blazor's script the page stays on the screen that shows before .NET runs
		await Page.RouteAsync("**/_framework/blazor.webassembly*", route => route.AbortAsync());
		await Page.ReloadAsync();

		await Expect(Page.GetByTestId("startup-label")).ToHaveTextAsync("Ihr Arbeitsbereich wird vorbereitet …");
	}

	[Fact]
	public async Task TheServerAnswersInTheChosenLanguageOverTheBrowsers()
	{
		await Page.GotoAsync(App.BaseUrl);
		await ChooseGermanAsync();

		await OpenAsync();
		await FillValidAsync();
		await Input("name").FillAsync("Reserved");
		await SubmitAsync();

		await Expect(Error("name")).ToHaveTextAsync("Der Name ist reserviert.");
	}

	private async Task ChooseGermanAsync()
	{
		await Page.GetByTestId("language-switch").GetByText("DE", new LocatorGetByTextOptions { Exact = true, }).ClickAsync();
		await Expect(Heading("Willkommen")).ToBeVisibleAsync();
	}

	private ILocator Heading(string name) => Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = name, });
}
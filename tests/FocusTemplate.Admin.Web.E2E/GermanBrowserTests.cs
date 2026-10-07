using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

// A browser whose language is German gets the app in German on its first visit, its messages and the server's too
[Collection(AspireCollection.Name)]
public sealed class GermanBrowserTests(BlazorAppFixture app) : ValidationDemoTest(app)
{
	public override BrowserNewContextOptions ContextOptions()
	{
		BrowserNewContextOptions options = base.ContextOptions();
		options.Locale = "de-AT";
		return options;
	}

	[Fact]
	public async Task TheAppSpeaksTheLanguageOfTheBrowser()
	{
		await Page.GotoAsync(App.BaseUrl);

		await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Willkommen", })).ToBeVisibleAsync();
		await Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "de-AT");
	}

	[Fact]
	public async Task TheBrowsersChecksSpeakGerman()
	{
		await OpenAsync();

		await TypeAsync("code", "abc");
		await TypeAsync("nickname", "Ada12");

		await Expect(Error("code")).ToHaveTextAsync("Drei Großbuchstaben.");
		await Expect(Error("nickname")).ToHaveTextAsync("Der Spitzname darf keine Ziffern enthalten.");
	}

	[Fact]
	public async Task TheLoadingScreenSpeaksTheLanguageOfTheBrowserToo()
	{
		// Without Blazor's script the page stays on the screen that shows before .NET runs
		await Page.RouteAsync("**/_framework/blazor.webassembly*", route => route.AbortAsync());

		await Page.GotoAsync(App.BaseUrl);

		await Expect(Page.GetByTestId("startup-label")).ToHaveTextAsync("Ihr Arbeitsbereich wird vorbereitet …");
		await Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "de-AT");
	}

	[Fact]
	public async Task TheServerAnswersInGermanToo()
	{
		await OpenAsync();
		await CheckOnlyOnTheServerAsync();
		await FillValidAsync();
		await Input("code").FillAsync("abc");

		await SubmitAsync();

		await Expect(Error("code")).ToHaveTextAsync("Drei Großbuchstaben.");
	}
}
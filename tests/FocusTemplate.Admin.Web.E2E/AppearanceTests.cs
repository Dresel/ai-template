using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

[Collection(AspireCollection.Name)]
public sealed class AppearanceTests(AdminAppFixture app) : BffPageTest(app)
{
	// The page background of Radzen's software theme and of software-dark, which the loading screen shares
	private const string Dark = "rgb(40, 54, 60)";

	private const string Light = "rgb(246, 247, 250)";

	private ILocator Body => Page.Locator("body");

	private ILocator Toggle => Page.GetByTestId("theme-toggle");

	[Fact]
	public async Task AChoiceHoldsAcrossLoadsWhateverTheSystemPrefers()
	{
		await PreferAsync(ColorScheme.Dark);
		await OpenAsync();

		// The button steps from System to Light, to Dark and back
		await Toggle.ClickAsync();
		await Expect(Toggle).ToHaveAttributeAsync("aria-label", "Appearance: Light");
		await Expect(Body).ToHaveCSSAsync("background-color", Light);

		await Page.ReloadAsync();
		await Expect(Page.GetByTestId("top-bar")).ToBeVisibleAsync();

		await Expect(Body).ToHaveCSSAsync("background-color", Light);
		await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", "light");
		await Expect(Toggle).ToHaveAttributeAsync("aria-label", "Appearance: Light");
	}

	[Fact]
	public async Task OnSystemTheAppFollowsAChangeWhileItIsOpen()
	{
		await PreferAsync(ColorScheme.Light);
		await OpenAsync();
		await Expect(Body).ToHaveCSSAsync("background-color", Light);

		await PreferAsync(ColorScheme.Dark);

		await Expect(Body).ToHaveCSSAsync("background-color", Dark);
	}

	[Fact]
	public async Task TheButtonStepsThroughAllThreeAndBack()
	{
		await PreferAsync(ColorScheme.Light);
		await OpenAsync();
		await Expect(Toggle).ToHaveAttributeAsync("aria-label", "Appearance: System");

		await Toggle.ClickAsync();
		await Toggle.ClickAsync();
		await Expect(Toggle).ToHaveAttributeAsync("aria-label", "Appearance: Dark");
		await Expect(Body).ToHaveCSSAsync("background-color", Dark);

		await Toggle.ClickAsync();
		await Expect(Toggle).ToHaveAttributeAsync("aria-label", "Appearance: System");
		await Expect(Body).ToHaveCSSAsync("background-color", Light);
	}

	[Fact]
	public async Task TheLoadingScreenIsDarkBeforeTheAppRuns()
	{
		await PreferAsync(ColorScheme.Dark);

		// Without Blazor's script the page stays on the screen that shows before .NET runs
		await Page.RouteAsync("**/_framework/blazor.webassembly*", route => route.AbortAsync());
		Task<IResponse> logo = Page.WaitForResponseAsync(response => response.Url.Contains("/images/logo", StringComparison.Ordinal));
		await Page.GotoAsync(App.BaseUrl);

		await Expect(Page.Locator(".app-startup")).ToHaveCSSAsync("background-color", Dark);

		// The fallback route answers an unknown path with index.html, so a status alone proves nothing
		Assert.Equal("image/svg+xml", await (await logo).HeaderValueAsync("content-type"));
	}

	[Fact]
	public async Task WithoutAChoiceTheAppFollowsTheSystem()
	{
		await PreferAsync(ColorScheme.Dark);
		await OpenAsync();

		await Expect(Body).ToHaveCSSAsync("background-color", Dark);
		await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", "dark");
	}

	private async Task OpenAsync()
	{
		await Page.GotoAsync(App.BaseUrl);
		await Expect(Page.GetByTestId("top-bar")).ToBeVisibleAsync();
	}

	private Task PreferAsync(ColorScheme scheme) => Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = scheme, });
}
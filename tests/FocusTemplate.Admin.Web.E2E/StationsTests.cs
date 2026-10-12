using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

// As the developer, whom the weather seed makes the owner of both stations. The new name is one of its own, since the
// whole collection shares one database.
[Collection(AspireCollection.Name)]
public sealed class StationsTests(BlazorAppFixture app) : BffPageTest(app)
{
	[Fact]
	public async Task TheMenuLeadsToTheStations()
	{
		await Page.GotoAsync(App.BaseUrl);
		await Page.GetByTestId("nav-stations").ClickAsync(new LocatorClickOptions { Timeout = 30_000, });

		await Expect(Page).ToHaveURLAsync($"{App.BaseUrl}weather/stations");
		await Expect(Page.GetByTestId("station-row")).ToHaveCountAsync(2);
	}

	[Fact]
	public async Task TheOwnerSeesTheirStationsAndRenamesOne()
	{
		string name = $"E2E {Guid.NewGuid():N}";

		await Page.GotoAsync($"{App.BaseUrl}weather/stations");
		await Expect(Page.GetByTestId("stations-table")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000, });

		ILocator vienna = Page.GetByTestId("station-row").Filter(new LocatorFilterOptions { HasText = "VIE", });
		await Expect(vienna).ToContainTextAsync(BlazorAppFixture.DisplayName);

		await vienna.GetByTestId("rename-station").ClickAsync();
		await Page.GetByTestId("station-name").FillAsync(name);
		await Page.GetByTestId("save-station").ClickAsync();

		await Expect(vienna).ToContainTextAsync(name);
	}
}
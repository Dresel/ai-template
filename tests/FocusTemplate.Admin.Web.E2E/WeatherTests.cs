using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

[Collection(AspireCollection.Name)]
public sealed class WeatherTests(BlazorAppFixture app) : BffPageTest
{
	[Fact]
	public async Task WeatherPageRendersSeededForecast()
	{
		await Page.GotoAsync($"{app.BaseUrl}weather");

		await Expect(Page.GetByTestId("weather-table")).ToBeVisibleAsync();

		ILocator rows = Page.GetByTestId("weather-row");
		await Expect(rows).ToHaveCountAsync(5);

		await Expect(rows.First).ToContainTextAsync("18");
		await Expect(rows.First).ToContainTextAsync("Mild");
		await Expect(rows.Last).ToContainTextAsync("31");
		await Expect(rows.Last).ToContainTextAsync("Scorching");
	}
}
namespace FocusTemplate.Web.E2E;

[Collection(AspireCollection.Name)]
public sealed class WeatherTests(BlazorAppFixture app) : BffPageTest
{
	[Fact]
	public async Task WeatherPageRendersForecastRows()
	{
		await Page.GotoAsync($"{app.BaseUrl}weather");

		await Expect(Page.GetByTestId("weather-table")).ToBeVisibleAsync();
		await Expect(Page.GetByTestId("weather-row")).ToHaveCountAsync(5);
	}
}
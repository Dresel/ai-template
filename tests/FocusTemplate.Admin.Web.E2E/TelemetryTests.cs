using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

[Collection(AspireCollection.Name)]
public sealed class TelemetryTests(AdminAppFixture app) : BffPageTest(app)
{
	// The test AppHost runs no dashboard, so the BFF answers an export with a 502. The test checks the browser's half:
	// the page keeps working while its exporters send OTLP over the BFF's origin.
	[Theory]
	[InlineData("traces")]
	[InlineData("logs")]
	public async Task TheBrowserExportsItsTelemetryThroughTheBff(string signal)
	{
		Task<IRequest> exported = Page.WaitForRequestAsync(
			request => request.Method == "POST" && request.Url.EndsWith($"/_otlp/v1/{signal}", StringComparison.Ordinal),
			new PageWaitForRequestOptions { Timeout = 30_000, });

		await Page.GotoAsync($"{App.BaseUrl}weather");
		await Expect(Page.GetByTestId("weather-table")).ToBeVisibleAsync();

		IRequest request = await exported;
		Assert.StartsWith(App.BaseUrl, request.Url, StringComparison.Ordinal);
		Assert.Equal("application/x-protobuf", await request.HeaderValueAsync("content-type"));
	}
}
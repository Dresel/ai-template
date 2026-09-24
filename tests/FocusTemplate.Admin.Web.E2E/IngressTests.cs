using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace FocusTemplate.Admin.Web.E2E;

[Collection(IngressCollection.Name)]
public sealed class IngressTests(IngressAppFixture app) : PageTest
{
	public override BrowserNewContextOptions ContextOptions() => new() { IgnoreHTTPSErrors = true, };

	[Fact]
	public async Task LoginThroughTheIngressLandsBackOnTheIngress()
	{
		// Keycloak sends the browser to the redirect_uri the BFF builds from the Host header nginx forwards.
		// The ingress listens on a random port, which that header has to keep.
		await Page.GotoAsync($"{app.BaseUrl}weather");
		await BlazorAppFixture.LogInAsync(Page);

		await Expect(Page).ToHaveURLAsync($"{app.BaseUrl}weather");
		await Expect(Page.GetByTestId("user-name")).ToHaveTextAsync(BlazorAppFixture.Username);
	}
}
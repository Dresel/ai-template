using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace FocusTemplate.Admin.Web.E2E;

[Collection(IngressCollection.Name)]
public sealed class IngressTests(IngressAppFixture app) : PageTest
{
	public override BrowserNewContextOptions ContextOptions() => new() { IgnoreHTTPSErrors = true, };

	// Through nginx with TLS and HTTP/2, the way the cluster's ingress serves the streams
	[Fact]
	public async Task AGroupCreatedInOneTabAppearsInTheOtherThroughTheIngress()
	{
		string name = $"E2E {Guid.NewGuid():N}";
		await Page.GotoAsync($"{app.BaseUrl}user-management/groups");
		await BlazorAppFixture.LogInAsync(Page);
		IPage watching = await Context.NewPageAsync();
		await LiveGroupsList.OpenAsync(watching, $"{app.BaseUrl}user-management/groups");

		await Expect(Page.GetByTestId("new-group")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000, });
		await Page.GetByTestId("new-group").ClickAsync();
		await Page.GetByTestId("group-name").FillAsync(name);
		await Page.GetByTestId("save-group").ClickAsync();
		await Expect(Page.GetByTestId("group-title")).ToHaveTextAsync(name);

		await Expect(watching.GetByTestId("group-row").Filter(new LocatorFilterOptions { HasText = name, }))
			.ToHaveCountAsync(1);
	}

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
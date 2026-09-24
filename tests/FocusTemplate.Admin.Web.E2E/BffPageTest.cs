using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace FocusTemplate.Admin.Web.E2E;

public abstract class BffPageTest(BlazorAppFixture app) : PageTest
{
	protected BlazorAppFixture App { get; } = app;

	// Every context starts signed in from the fixture's session. Keycloak serves the ASP.NET dev certificate, which the
	// test browser does not trust.
	public override BrowserNewContextOptions ContextOptions() =>
		new() { IgnoreHTTPSErrors = true, StorageState = App.StorageState, };
}
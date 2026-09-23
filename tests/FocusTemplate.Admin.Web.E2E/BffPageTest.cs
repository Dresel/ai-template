using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace FocusTemplate.Admin.Web.E2E;

public abstract class BffPageTest : PageTest
{
	// The BFF serves the ASP.NET dev certificate, which the test browser does not trust.
	public override BrowserNewContextOptions ContextOptions() => new() { IgnoreHTTPSErrors = true };
}
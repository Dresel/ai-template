using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace FocusTemplate.Web.E2E;

public abstract class BffPageTest : PageTest
{
	// Ignore dev certificate errors
	public override BrowserNewContextOptions ContextOptions() => new() { IgnoreHTTPSErrors = true };
}
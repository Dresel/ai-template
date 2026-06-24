using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace FocusTemplate.Web.E2E;

public sealed class CounterTests(BlazorAppFixture app) : PageTest, IClassFixture<BlazorAppFixture>
{
	[Fact]
	public async Task ClickingTheButtonIncrementsTheCounter()
	{
		await Page.GotoAsync(app.BaseUrl);

		ILocator counter = Page.GetByTestId("counter-value");
		await Expect(counter).ToHaveTextAsync("0");

		ILocator increment = Page.GetByTestId("counter-increment");

		await increment.ClickAsync();
		await Expect(counter).ToHaveTextAsync("1");

		await increment.ClickAsync();
		await Expect(counter).ToHaveTextAsync("2");
	}
}
using Microsoft.Playwright;

namespace FocusTemplate.Web.E2E;

[Collection(AspireCollection.Name)]
public sealed class CounterTests(BlazorAppFixture app) : BffPageTest
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
namespace Argus.Web.E2E;

[Collection(AspireCollection.Name)]
public sealed class LaunchesTests(BlazorAppFixture app) : BffPageTest
{
	[Fact]
	public async Task LaunchesPageRendersEmptyFeed()
	{
		// The E2E AppHost runs without the intel listener, so the feed is deterministically empty.
		await Page.GotoAsync($"{app.BaseUrl}launches");

		await Expect(Page.GetByTestId("launches-tokens-only")).ToBeVisibleAsync();
		await Expect(Page.GetByTestId("launches-empty")).ToBeVisibleAsync();
	}
}
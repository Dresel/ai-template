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

	[Fact]
	public async Task HomePageShowsTheLaunchFeed()
	{
		await Page.GotoAsync(app.BaseUrl);

		await Expect(Page.GetByTestId("launches-tokens-only")).ToBeVisibleAsync();
		await Expect(Page.GetByTestId("launches-empty")).ToBeVisibleAsync();
	}

	[Fact]
	public async Task SearchingReportsNoMatchesInsteadOfAnEmptyFeed()
	{
		await Page.GotoAsync(app.BaseUrl);

		await Expect(Page.GetByTestId("launches-empty")).ToHaveTextAsync("No deployments detected yet.");

		// Real keystrokes, so the debounced ValueChanged binding runs the query as it does for a user.
		await Page.GetByTestId("launches-search").ClickAsync();
		await Page.GetByTestId("launches-search").PressSequentiallyAsync("karma");

		await Expect(Page.GetByTestId("launches-empty")).ToHaveTextAsync("No deployments match this search.");
	}
}
using FocusTemplate.Admin.Client.UserManagement.Groups;

namespace FocusTemplate.Admin.Web.UnitTests.Features.UserManagement.Groups;

public sealed class GroupsClientTests
{
	[Fact]
	public async Task AWatchBypassesTheBrowserCache()
	{
		using TestWatchHandler handler = new();
		GroupsClient api = new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/"), });

		await api.WatchAsync(TestContext.Current.CancellationToken);

		Assert.True(handler.Request!.Options.TryGetValue(
			new HttpRequestOptionsKey<IDictionary<string, object>>("WebAssemblyFetchOptions"),
			out IDictionary<string, object>? fetch));
		Assert.Equal("no-store", fetch["cache"]);
	}
}
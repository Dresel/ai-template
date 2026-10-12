using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

internal static class LiveGroupsList
{
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

	// Returns once the list's stream is open and the reload its connect started has come back, so a change made after
	// it can reach the page only as an event
	public static async Task OpenAsync(IPage page, string url)
	{
		bool open = false;
		TaskCompletionSource<IRequest> reload = new(TaskCreationOptions.RunContinuationsAsynchronously);
		page.Response += (_, response) => open |= response.Url.EndsWith("/groups:watch", StringComparison.Ordinal);
		page.Request += (_, request) =>
		{
			if (open && request.Method == "GET" && request.Url.EndsWith("/groups", StringComparison.Ordinal))
			{
				reload.TrySetResult(request);
			}
		};

		await page.GotoAsync(url);
		IRequest reloading = await reload.Task.WaitAsync(Patience);
		await reloading.ResponseAsync();
	}
}
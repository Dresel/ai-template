using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FocusTemplate.Admin.Shared.UserManagement;

namespace FocusTemplate.Admin.Client.UserManagement.Groups;

// GET /groups:watch by hand, until the emitter generates server-sent event operations
public sealed partial class GroupsClient
{
	// The fetch options of Blazor's browser handler, the keys its SetBrowserRequestCache uses
	private static readonly HttpRequestOptionsKey<IDictionary<string, object>> BrowserFetchOptions = new("WebAssemblyFetchOptions");

	// The browser hands the body over as it arrives, instead of at its end, which a stream never reaches
	private static readonly HttpRequestOptionsKey<bool> BrowserResponseStreaming = new("WebAssemblyEnableStreamingResponse");

	// Returns once the stream is open, so the caller knows from when on it misses nothing
	public async Task<IAsyncEnumerable<GroupChanged>> WatchAsync(CancellationToken cancellationToken = default)
	{
		// "./" because a colon in the first segment would read as a URI scheme
		HttpRequestMessage request = new(HttpMethod.Get, ApiClientSupport.RelativeUri("./groups:watch"));
		request.Options.Set(BrowserResponseStreaming, true);

		// Past the browser's HTTP cache: Firefox holds a second request for a URL there until the first one ends, which a
		// stream in another tab never does
		request.Options.Set(BrowserFetchOptions, new Dictionary<string, object>(StringComparer.Ordinal) { ["cache"] = "no-store", });

		HttpResponseMessage response =
			await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			using (request)
			using (response)
			{
				throw await ApiClientSupport.UnexpectedResponseAsync(response, cancellationToken).ConfigureAwait(false);
			}
		}

		return ReadAsync(request, response, jsonOptions.GetTypeInfo<GroupChanged>(), cancellationToken);
	}

	// Skips the event types it does not know, the keepalive among them
	private static async IAsyncEnumerable<GroupChanged> ReadAsync(
		HttpRequestMessage request,
		HttpResponseMessage response,
		JsonTypeInfo<GroupChanged> typeInfo,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		using (request)
		using (response)
		{
			await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
			SseParser<GroupChanged?> parser = SseParser.Create(
				body,
				(eventType, data) => eventType == "groupChanged" ? JsonSerializer.Deserialize(data, typeInfo) : null);

			await foreach (SseItem<GroupChanged?> item in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
			{
				if (item.Data is { } change)
				{
					yield return change;
				}
			}
		}
	}
}
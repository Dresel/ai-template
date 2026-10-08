using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;

namespace FocusTemplate.Admin.Api.Streams;

public static class ServerSentEventStreams
{
	// Below nginx's 60 s proxy_read_timeout and YARP's 100 s activity timeout, which close an idle response
	private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(20);

	// Each event goes out under its type, between keepalive events the clients skip as a type they do not know. The
	// source starts here, before the result opens the response: it subscribes before the client sees the stream open
	public static IAsyncEnumerable<SseItem<T?>> WithKeepAlive<T>(IAsyncEnumerable<T> events, string eventType, CancellationToken cancellationToken)
		where T : class
	{
		IAsyncEnumerator<T> enumerator = events.GetAsyncEnumerator(cancellationToken);
		Task<bool> next = enumerator.MoveNextAsync().AsTask();

		return ContinueAsync(enumerator, next, eventType, cancellationToken);
	}

	private static async IAsyncEnumerable<SseItem<T?>> ContinueAsync<T>(
		IAsyncEnumerator<T> enumerator,
		Task<bool> next,
		string eventType,
		[EnumeratorCancellation] CancellationToken cancellationToken)
		where T : class
	{
		try
		{
			// The first item opens the response, which the client waits for before it reads anything
			yield return new SseItem<T?>(null, "keepalive");

			Task tick = Task.Delay(KeepAliveInterval, cancellationToken);
			while (true)
			{
				if (await Task.WhenAny(next, tick) == tick)
				{
					cancellationToken.ThrowIfCancellationRequested();
					yield return new SseItem<T?>(null, "keepalive");
					tick = Task.Delay(KeepAliveInterval, cancellationToken);
					continue;
				}

				if (!await next)
				{
					yield break;
				}

				yield return new SseItem<T?>(enumerator.Current, eventType);
				next = enumerator.MoveNextAsync().AsTask();
			}
		}
		finally
		{
			// An async iterator refuses DisposeAsync while a MoveNextAsync runs, which the ended request cancels
			await ((Task)next).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
			await enumerator.DisposeAsync();
		}
	}
}
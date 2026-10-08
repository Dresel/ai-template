using System.Net;

namespace FocusTemplate.Admin.Web.Foundation;

// One server-sent event stream for the pages of a tab, so a singleton. It runs only while a page listens, since over
// plain HTTP/1.1 every open stream holds one of the six connections a browser allows a host. It reconnects after any
// end and reports it, because the signals in between are lost and the pages read everything again
public sealed class ChangeFeed<T>(Func<CancellationToken, Task<IAsyncEnumerable<T>>> watch, TimeProvider clock) : IAsyncDisposable
{
	private static readonly TimeSpan FirstRetry = TimeSpan.FromSeconds(1);

	private static readonly TimeSpan LongestRetry = TimeSpan.FromSeconds(30);

	private readonly List<Listener> listeners = [];

	private Task running = Task.CompletedTask;

	private CancellationTokenSource? stopping;

	public async ValueTask DisposeAsync()
	{
		lock (this.listeners)
		{
			this.listeners.Clear();
			Stop();
		}

		await this.running.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
	}

	// The first listener opens the stream, and disposing the last one's subscription closes it
	public IDisposable Listen(Action<T> changed, Action reconnected)
	{
		Listener listener = new(this, changed, reconnected);
		lock (this.listeners)
		{
			this.listeners.Add(listener);
			if (this.stopping is null)
			{
				this.stopping = new CancellationTokenSource();
				this.running = RunAsync(this.stopping.Token);
			}
		}

		return listener;
	}

	private Listener[] Current()
	{
		lock (this.listeners)
		{
			return [.. this.listeners,];
		}
	}

	private void Leave(Listener listener)
	{
		lock (this.listeners)
		{
			if (this.listeners.Remove(listener) && this.listeners.Count == 0)
			{
				Stop();
			}
		}
	}

	private async Task RunAsync(CancellationToken cancellationToken)
	{
		TimeSpan retry = FirstRetry;
		bool gap = false;
		while (true)
		{
			try
			{
				IAsyncEnumerable<T> changes = await watch(cancellationToken);
				if (gap)
				{
					foreach (Listener listener in Current())
					{
						listener.Reconnected();
					}
				}

				retry = FirstRetry;
				await foreach (T change in changes.WithCancellation(cancellationToken))
				{
					foreach (Listener listener in Current())
					{
						listener.Changed(change);
					}
				}
			}
			catch (HttpRequestException exception) when (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
			{
				// Without the session or the permission no retry succeeds
				return;
			}
			catch (Exception exception) when (exception is HttpRequestException or IOException)
			{
			}

			// From the first end or failure on, the stream missed signals the pages loaded around
			gap = true;
			await Task.Delay(retry, clock, cancellationToken);
			retry = retry * 2 < LongestRetry ? retry * 2 : LongestRetry;
		}
	}

	private void Stop()
	{
		this.stopping?.Cancel();
		this.stopping?.Dispose();
		this.stopping = null;
	}

	private sealed class Listener(ChangeFeed<T> feed, Action<T> changed, Action reconnected) : IDisposable
	{
		public Action<T> Changed { get; } = changed;

		public Action Reconnected { get; } = reconnected;

		public void Dispose() => feed.Leave(this);
	}
}
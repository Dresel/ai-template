using System.Net;

namespace FocusTemplate.Admin.Web.Foundation;

// One server-sent event stream for one page's view model, so scoped: the page resolves its view model from a scope of
// its own and disposes it with the page. It reconnects after any end and reports every connect
public sealed partial class LiveUpdates<T> : IDisposable
{
	private static readonly TimeSpan FirstRetry = TimeSpan.FromSeconds(1);

	private static readonly TimeSpan LongestRetry = TimeSpan.FromSeconds(30);

	private readonly CancellationTokenSource stopping = new();

	private readonly Func<CancellationToken, Task<IAsyncEnumerable<T>>> watch;

	private readonly TimeProvider clock;

	private readonly ILogger<LiveUpdates<T>> logger;

	public LiveUpdates(Func<CancellationToken, Task<IAsyncEnumerable<T>>> watch, TimeProvider clock, ILogger<LiveUpdates<T>> logger)
	{
		this.watch = watch;
		this.clock = clock;
		this.logger = logger;

		// The view model it serves subscribes right after resolving it, and a signal can only follow the connect
		_ = RunAsync(this.stopping.Token);
	}

	public event Action<T>? Received;

	// The server subscribed the stream before it answered, so whatever a page reads from now on misses nothing
	public event Action? Connected;

	public void Dispose()
	{
		this.stopping.Cancel();
		this.stopping.Dispose();
	}

	[LoggerMessage(LogLevel.Error, "A handler of live updates failed.")]
	private static partial void LogHandlerFailure(ILogger logger, Exception exception);

	// One by one, so a handler that throws keeps the others and the stream running
	private void Raise<THandler>(THandler? handlers, Action<THandler> invoke)
		where THandler : Delegate
	{
		foreach (THandler handler in handlers?.GetInvocationList().Cast<THandler>() ?? [])
		{
			try
			{
				invoke(handler);
			}
			catch (Exception exception)
			{
				LogHandlerFailure(this.logger, exception);
			}
		}
	}

	private async Task RunAsync(CancellationToken cancellationToken)
	{
		TimeSpan retry = FirstRetry;
		while (true)
		{
			try
			{
				IAsyncEnumerable<T> changes = await this.watch(cancellationToken);

				// The generated reader owns the connection, which every end closes
				await using (changes as IAsyncDisposable)
				{
					// A stream closed with its page can still finish a call underway, whose result goes to nobody
					if (!cancellationToken.IsCancellationRequested)
					{
						Raise(Connected, handler => handler());
					}

					retry = FirstRetry;
					await foreach (T change in changes.WithCancellation(cancellationToken))
					{
						if (cancellationToken.IsCancellationRequested)
						{
							return;
						}

						Raise(Received, handler => handler(change));
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

			await Task.Delay(retry, this.clock, cancellationToken);
			retry = retry * 2 < LongestRetry ? retry * 2 : LongestRetry;
		}
	}
}
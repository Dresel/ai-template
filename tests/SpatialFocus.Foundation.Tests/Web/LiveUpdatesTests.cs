using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using SpatialFocus.Web.Foundation;

namespace SpatialFocus.Foundation.Tests.Web;

public sealed class LiveUpdatesTests
{
	private readonly FakeTimeProvider clock = new();

	private readonly Channel<(Channel<int> Changes, CancellationToken Token, TaskCompletionSource Open)> connections =
		Channel.CreateUnbounded<(Channel<int> Changes, CancellationToken Token, TaskCompletionSource Open)>();

	private int attempts;

	private int closedStreams;

	[Fact]
	public async Task AForbiddenStreamStopsForGood()
	{
		using LiveUpdates<int> feed = Feed(_ =>
		{
			Interlocked.Increment(ref this.attempts);
			throw new HttpRequestException("Forbidden", null, HttpStatusCode.Forbidden);
		});

		for (int second = 0; second < 60; second++)
		{
			this.clock.Advance(TimeSpan.FromSeconds(1));
			await Task.Delay(5, TestContext.Current.CancellationToken);
		}

		Assert.Equal(1, this.attempts);
	}

	[Fact]
	public async Task TheFirstConnectAndEveryReconnectAreReported()
	{
		List<int> changes = [];
		int connects = 0;
		using LiveUpdates<int> feed = Feed(ConnectAsync);

		feed.Received += changes.Add;
		feed.Connected += () => connects++;
		Channel<int> first = (await NextConnectionAsync()).Changes;
		await WaitUntilAsync(() => connects == 1);
		await first.Writer.WriteAsync(1, TestContext.Current.CancellationToken);
		first.Writer.Complete();
		await WaitUntilAsync(() => changes.Count == 1);
		bool reconnectedBeforeTheRetry = connects > 1;
		Channel<int> second = (await NextConnectionAsync()).Changes;
		await second.Writer.WriteAsync(2, TestContext.Current.CancellationToken);
		await WaitUntilAsync(() => changes.Count == 2);

		Assert.False(reconnectedBeforeTheRetry);
		Assert.Equal(2, connects);
		Assert.Equal([1, 2,], changes);
	}

	[Fact]
	public async Task AStreamClosedWithItsPageHandsItNothing()
	{
		List<int> changes = [];
		LiveUpdates<int> feed = Feed(ConnectIgnoringCancellationAsync);

		feed.Received += changes.Add;
		Channel<int> closed = (await NextConnectionAsync()).Changes;
		feed.Dispose();

		// The closed stream was in a read that ignores the cancellation, and that read now returns a change
		await closed.Writer.WriteAsync(1, TestContext.Current.CancellationToken);
		await WaitUntilAsync(() => this.closedStreams > 0 || changes.Count > 0);
		closed.Writer.Complete();

		Assert.Empty(changes);
	}

	[Fact]
	public async Task AThrowingHandlerKeepsTheOthersAndTheStreamRunning()
	{
		List<int> changes = [];
		using LiveUpdates<int> feed = Feed(ConnectAsync);

		feed.Received += _ => throw new InvalidOperationException("A handler failed");
		feed.Received += changes.Add;
		Channel<int> connection = (await NextConnectionAsync()).Changes;
		await connection.Writer.WriteAsync(1, TestContext.Current.CancellationToken);
		await connection.Writer.WriteAsync(2, TestContext.Current.CancellationToken);
		await WaitUntilAsync(() => changes.Count == 2);

		Assert.Equal([1, 2,], changes);
	}

	[Fact]
	public async Task TheStreamClosesWithItsPage()
	{
		LiveUpdates<int> feed = Feed(ConnectAsync);

		CancellationToken stream = (await NextConnectionAsync()).Token;
		feed.Dispose();

		Assert.True(stream.IsCancellationRequested);
	}

	private static async IAsyncEnumerable<int> ReadAsync(
		Channel<int> connection,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		await foreach (int change in connection.Reader.ReadAllAsync(cancellationToken))
		{
			yield return change;
		}
	}

	private static async Task WaitUntilAsync(Func<bool> condition)
	{
		using CancellationTokenSource patience = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		patience.CancelAfter(TimeSpan.FromSeconds(5));
		while (!condition())
		{
			await Task.Delay(5, patience.Token);
		}
	}

	private LiveUpdates<int> Feed(Func<CancellationToken, Task<IAsyncEnumerable<int>>> watch) =>
		new(watch, this.clock, NullLogger<LiveUpdates<int>>.Instance);

	// Each connection is a channel the test writes the stream's changes into, and completes to end the stream
	private async Task<IAsyncEnumerable<int>> ConnectAsync(CancellationToken cancellationToken)
	{
		Channel<int> connection = await OpenAsync(cancellationToken);

		return ReadAsync(connection, cancellationToken);
	}

	// A read that ignores the stream's cancellation, as one already waiting on the network can, and counts its closing
	private async IAsyncEnumerable<int> ReadIgnoringCancellationAsync(Channel<int> connection)
	{
		try
		{
			await foreach (int change in connection.Reader.ReadAllAsync(CancellationToken.None))
			{
				yield return change;
			}
		}
		finally
		{
			Interlocked.Increment(ref this.closedStreams);
		}
	}

	private async Task<IAsyncEnumerable<int>> ConnectIgnoringCancellationAsync(CancellationToken cancellationToken) =>
		ReadIgnoringCancellationAsync(await OpenAsync(cancellationToken));

	// Completes when the test picks the connection up, as an HTTP connect completes only after the caller moved on
	private async Task<Channel<int>> OpenAsync(CancellationToken cancellationToken)
	{
		Channel<int> connection = Channel.CreateUnbounded<int>();
		TaskCompletionSource open = new();
		await this.connections.Writer.WriteAsync((connection, cancellationToken, open), cancellationToken);
		await open.Task.WaitAsync(cancellationToken);

		return connection;
	}

	// Advances the clock while it waits, since the delay before a retry may start only after the test moved on
	private async Task<(Channel<int> Changes, CancellationToken Token)> NextConnectionAsync()
	{
		using CancellationTokenSource patience = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		patience.CancelAfter(TimeSpan.FromSeconds(5));
		(Channel<int> Changes, CancellationToken Token, TaskCompletionSource Open) connection;
		while (!this.connections.Reader.TryRead(out connection))
		{
			this.clock.Advance(TimeSpan.FromSeconds(1));
			await Task.Delay(5, patience.Token);
		}

		connection.Open.SetResult();

		return (connection.Changes, connection.Token);
	}
}
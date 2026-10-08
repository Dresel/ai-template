using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using FocusTemplate.Admin.Web.Foundation;
using Microsoft.Extensions.Time.Testing;

namespace FocusTemplate.Admin.Web.UnitTests.Foundation;

public sealed class ChangeFeedTests
{
	private readonly FakeTimeProvider clock = new();

	private readonly Channel<(Channel<int> Changes, CancellationToken Token)> connections =
		Channel.CreateUnbounded<(Channel<int> Changes, CancellationToken Token)>();

	private int attempts;

	[Fact]
	public async Task AForbiddenStreamStopsForGood()
	{
		await using ChangeFeed<int> feed = new(
			_ =>
			{
				Interlocked.Increment(ref this.attempts);
				throw new HttpRequestException("Forbidden", null, HttpStatusCode.Forbidden);
			},
			this.clock);

		using IDisposable listening = feed.Listen(_ => { }, () => { });
		for (int second = 0; second < 60; second++)
		{
			this.clock.Advance(TimeSpan.FromSeconds(1));
			await Task.Delay(5, TestContext.Current.CancellationToken);
		}

		Assert.Equal(1, this.attempts);
	}

	[Fact]
	public async Task AnEndedStreamReconnectsAndReportsTheGap()
	{
		List<int> changes = [];
		int reconnects = 0;
		await using ChangeFeed<int> feed = new(ConnectAsync, this.clock);

		using IDisposable listening = feed.Listen(changes.Add, () => reconnects++);
		Channel<int> first = (await NextConnectionAsync()).Changes;
		await first.Writer.WriteAsync(1, TestContext.Current.CancellationToken);
		first.Writer.Complete();
		await WaitUntilAsync(() => changes.Count == 1);
		bool reconnectedBeforeTheRetry = reconnects > 0;
		Channel<int> second = (await NextConnectionAsync()).Changes;
		await second.Writer.WriteAsync(2, TestContext.Current.CancellationToken);
		await WaitUntilAsync(() => changes.Count == 2);

		Assert.False(reconnectedBeforeTheRetry);
		Assert.Equal(1, reconnects);
		Assert.Equal([1, 2,], changes);
	}

	[Fact]
	public async Task TheStreamClosesWhenTheLastListenerLeaves()
	{
		await using ChangeFeed<int> feed = new(ConnectAsync, this.clock);

		IDisposable first = feed.Listen(_ => { }, () => { });
		IDisposable second = feed.Listen(_ => { }, () => { });
		CancellationToken stream = (await NextConnectionAsync()).Token;
		first.Dispose();
		bool closedWithAListenerLeft = stream.IsCancellationRequested;
		second.Dispose();

		Assert.False(closedWithAListenerLeft);
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

	// Each connection is a channel the test writes the stream's changes into, and completes to end the stream
	private async Task<IAsyncEnumerable<int>> ConnectAsync(CancellationToken cancellationToken)
	{
		Channel<int> connection = Channel.CreateUnbounded<int>();
		await this.connections.Writer.WriteAsync((connection, cancellationToken), cancellationToken);

		return ReadAsync(connection, cancellationToken);
	}

	// Advances the clock while it waits, since the delay before a retry may start only after the test moved on
	private async Task<(Channel<int> Changes, CancellationToken Token)> NextConnectionAsync()
	{
		using CancellationTokenSource patience = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		patience.CancelAfter(TimeSpan.FromSeconds(5));
		(Channel<int> Changes, CancellationToken Token) connection;
		while (!this.connections.Reader.TryRead(out connection))
		{
			this.clock.Advance(TimeSpan.FromSeconds(1));
			await Task.Delay(5, patience.Token);
		}

		return connection;
	}
}
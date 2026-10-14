using System.Runtime.CompilerServices;
using System.Threading.Channels;
using SpatialFocus.Primitives;

namespace SpatialFocus.Api.Foundation.Realtime;

// One open stream, registered from EventStreams.Subscribe until disposed
public sealed class EventSubscription<TSignal> : IDisposable
{
	// A stream that falls this far behind ends, and its client reconnects and reads everything again
	private const int Capacity = 100;

	// The longest a stream stays open whatever its token says, so a token without exp ends too, and so does a stream
	// whose subscriber's permissions changed outside the handlers
	private static readonly TimeSpan MaxLifetime = TimeSpan.FromMinutes(15);

	// Wait refuses TryWrite on a full channel, so Offer ends the stream. A drop mode would accept the write and lose
	// the signal without the client noticing
	private readonly Channel<TSignal> channel = Channel.CreateBounded<TSignal>(
		new BoundedChannelOptions(Capacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait, });

	private readonly TimeProvider clock;

	private readonly DateTimeOffset end;

	private readonly EventStreams<TSignal> streams;

	private readonly ITimer timer;

	internal EventSubscription(EventStreams<TSignal> streams, TimeProvider clock, UserId subscriber, DateTimeOffset? until)
	{
		this.streams = streams;
		this.clock = clock;
		Subscriber = subscriber;

		DateTimeOffset now = clock.GetUtcNow();
		this.end = CalculateExpiration(now, until);

		// Fires once at the end and closes the channel for writes, which wakes a reader waiting on an empty one
		this.timer = clock.CreateTimer(_ => Close(), null, this.end - now, Timeout.InfiniteTimeSpan);
	}

	internal UserId Subscriber { get; }

	public void Dispose()
	{
		this.streams.Remove(this);
		this.timer.Dispose();
	}

	public async IAsyncEnumerable<TSignal> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
	{
		await foreach (TSignal signal in this.channel.Reader.ReadAllAsync(cancellationToken))
		{
			// The timer only closes the channel for writes, and the signals it still holds stay unsent after the end
			if (this.clock.GetUtcNow() >= this.end)
			{
				yield break;
			}

			yield return signal;
		}
	}

	// Closes the channel for writes, and the signals it holds still go out
	internal void Close() => this.channel.Writer.TryComplete();

	internal void Offer(TSignal signal)
	{
		if (!this.channel.Writer.TryWrite(signal))
		{
			Close();
		}
	}

	private static DateTimeOffset CalculateExpiration(DateTimeOffset now, DateTimeOffset? tokenExpiresAt)
	{
		DateTimeOffset maxExpiresAt = now + MaxLifetime;
		DateTimeOffset expiresAt = tokenExpiresAt ?? maxExpiresAt;

		return expiresAt < now ? now : expiresAt < maxExpiresAt ? expiresAt : maxExpiresAt;
	}
}
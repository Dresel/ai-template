using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace FocusTemplate.Admin.Api.Streams;

// The open server-sent event streams of this process. In memory, so a signal reaches only the streams of the replica
// that published it: enough while one API process serves them, Redis Pub/Sub follows with a second
public sealed class SignalHub<TSignal>(TimeProvider clock)
{
	// A stream that falls this far behind ends, and its client reconnects and reads everything again
	private const int Capacity = 100;

	private readonly ConcurrentDictionary<Channel<TSignal>, Func<TSignal, bool>> streams = new();

	public void Publish(TSignal signal)
	{
		foreach ((Channel<TSignal> stream, Func<TSignal, bool> audience) in this.streams)
		{
			if (audience(signal) && !stream.Writer.TryWrite(signal))
			{
				stream.Writer.TryComplete();
			}
		}
	}

	// Registers on the first MoveNextAsync and leaves when the reader stops, whatever ended the stream
	public async IAsyncEnumerable<TSignal> WatchAsync(
		Func<TSignal, bool> audience,
		DateTimeOffset? until,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		Channel<TSignal> stream = Channel.CreateBounded<TSignal>(new BoundedChannelOptions(Capacity) { SingleReader = true, });
		using ITimer? end = until is { } at
			? clock.CreateTimer(_ => stream.Writer.TryComplete(), null, Max(at - clock.GetUtcNow(), TimeSpan.Zero), Timeout.InfiniteTimeSpan)
			: null;

		this.streams[stream] = audience;
		try
		{
			await foreach (TSignal signal in stream.Reader.ReadAllAsync(cancellationToken))
			{
				yield return signal;
			}
		}
		finally
		{
			this.streams.TryRemove(stream, out _);
		}
	}

	private static TimeSpan Max(TimeSpan first, TimeSpan second) => first > second ? first : second;
}
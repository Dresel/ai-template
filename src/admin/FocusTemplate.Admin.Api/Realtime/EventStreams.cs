using System.Collections.Concurrent;
using FocusTemplate.Primitives;

namespace FocusTemplate.Admin.Api.Realtime;

// The open server-sent event streams of one signal type in this process
public sealed class EventStreams<TSignal>(TimeProvider clock) : IEventStreams
{
	// A set: the BCL has no concurrent one, so the value is unused
	private readonly ConcurrentDictionary<EventSubscription<TSignal>, byte> subscriptions = new();

	// Registered right away, so every signal delivered from now on reaches the subscription
	public EventSubscription<TSignal> Subscribe(UserId subscriber, DateTimeOffset? until)
	{
		EventSubscription<TSignal> subscription = new(this, clock, subscriber, until);
		this.subscriptions[subscription] = 0;

		return subscription;
	}

	public void Deliver(TSignal signal)
	{
		foreach (EventSubscription<TSignal> subscription in this.subscriptions.Keys)
		{
			subscription.Offer(signal);
		}
	}

	public void Close(IEnumerable<UserId> users)
	{
		HashSet<UserId> closing = [.. users,];
		foreach (EventSubscription<TSignal> subscription in this.subscriptions.Keys)
		{
			if (closing.Contains(subscription.Subscriber))
			{
				subscription.Close();
			}
		}
	}

	public void CloseAll()
	{
		foreach (EventSubscription<TSignal> subscription in this.subscriptions.Keys)
		{
			subscription.Close();
		}
	}

	internal void Remove(EventSubscription<TSignal> subscription) => this.subscriptions.TryRemove(subscription, out _);
}
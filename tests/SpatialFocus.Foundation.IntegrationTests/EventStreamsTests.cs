using Microsoft.Extensions.Time.Testing;
using SpatialFocus.Api.Foundation.Realtime;
using SpatialFocus.Primitives;

namespace SpatialFocus.Foundation.IntegrationTests;

// The streams alone on a clock the test moves, without the API host, its database and Redis
public sealed class EventStreamsTests
{
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

	private static readonly UserId Subscriber = UserId.From(Guid.CreateVersion7());

	private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero));

	[Theory]
	[InlineData(null)]
	[InlineData(60)]
	public async Task AStreamEndsAfterFifteenMinutesWithoutAnEarlierExpiry(int? expiresInMinutes)
	{
		EventStreams<int> streams = new(this.clock);
		DateTimeOffset? expiry = expiresInMinutes is { } minutes ? this.clock.GetUtcNow().AddMinutes(minutes) : null;
		using EventSubscription<int> subscription = streams.Subscribe(Subscriber, expiry);
		await using IAsyncEnumerator<int> stream = subscription.ReadAllAsync(TestContext.Current.CancellationToken)
			.GetAsyncEnumerator(TestContext.Current.CancellationToken);

		ValueTask<bool> next = stream.MoveNextAsync();
		this.clock.Advance(TimeSpan.FromMinutes(15));

		Assert.False(await next.AsTask().WaitAsync(Patience, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task SignalsStillBufferedWhenTheTokenExpiresAreNotDelivered()
	{
		EventStreams<int> streams = new(this.clock);
		using EventSubscription<int> subscription = streams.Subscribe(Subscriber, this.clock.GetUtcNow().AddMinutes(5));
		await using IAsyncEnumerator<int> stream = subscription.ReadAllAsync(TestContext.Current.CancellationToken)
			.GetAsyncEnumerator(TestContext.Current.CancellationToken);

		ValueTask<bool> first = stream.MoveNextAsync();
		streams.Deliver(1);
		streams.Deliver(2);
		streams.Deliver(3);
		Assert.True(await first);

		this.clock.Advance(TimeSpan.FromMinutes(5));

		Assert.False(await stream.MoveNextAsync().AsTask().WaitAsync(Patience, TestContext.Current.CancellationToken));
	}
}
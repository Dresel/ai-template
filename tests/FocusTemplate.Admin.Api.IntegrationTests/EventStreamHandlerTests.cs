using System.Security.Claims;
using FocusTemplate.Admin.Api.Realtime;
using FocusTemplate.Primitives;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FocusTemplate.Admin.Api.IntegrationTests;

// The handler alone, on streams of its own and a request with a signed-in subscriber
public sealed class EventStreamHandlerTests
{
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

	private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero));

	[Fact]
	public async Task SignalsPublishedWhileTheFilterLoadsPassThroughItOnceItIsReady()
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;
		EventStreams<int> streams = new(this.clock);
		TaskCompletionSource<Func<int, bool>> filter = new();
		EvenSignalsHandler handler = new(streams, filter.Task);
		await using IAsyncEnumerator<int> stream = handler.Handle(new Watch(), cancellationToken).GetAsyncEnumerator(cancellationToken);

		ValueTask<bool> first = stream.MoveNextAsync();
		streams.Deliver(1);
		streams.Deliver(2);
		filter.SetResult(signal => signal % 2 == 0);

		Assert.True(await first.AsTask().WaitAsync(Patience, cancellationToken));
		Assert.Equal(2, stream.Current);
	}

	private sealed record Watch : IStreamQuery<int>;

	// Its filter comes from the test, which completes it after the first signals went out
	private sealed class EvenSignalsHandler(EventStreams<int> streams, Task<Func<int, bool>> filter)
		: EventStreamHandler<Watch, int>(streams, RequestOf(UserId.From(Guid.CreateVersion7())))
	{
		protected override async ValueTask<Func<int, bool>> MatchesAsync(
			Watch query,
			UserId subscriber,
			CancellationToken cancellationToken) =>
			await filter;

		private static HttpContextAccessor RequestOf(UserId subscriber) =>
			new()
			{
				HttpContext = new DefaultHttpContext
				{
					User = new ClaimsPrincipal(
						new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, subscriber.Value.ToString()),], "Test")),
				},
			};
	}
}
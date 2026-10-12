using System.Runtime.CompilerServices;
using System.Security.Claims;
using FocusTemplate.Api.Foundation.Authentication;
using FocusTemplate.Primitives;
using Mediator;
using Microsoft.AspNetCore.Http;

namespace FocusTemplate.Api.Foundation.Realtime;

// Authorized when the stream opens. It ends with the access token, after 15 minutes at the latest, and when
// SignalBackplane.CloseStreamsFor closes it after a permission change, so each reconnect is authorized again
public abstract class EventStreamHandler<TQuery, TSignal>(EventStreams<TSignal> streams, IHttpContextAccessor httpContextAccessor)
	: IStreamQueryHandler<TQuery, TSignal>
	where TQuery : IStreamQuery<TSignal>
{
	public async IAsyncEnumerable<TSignal> Handle(TQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		ClaimsPrincipal user = httpContextAccessor.HttpContext?.User ??
			throw new InvalidOperationException("An event stream handler runs inside the request of its stream.");
		UserId subscriber = user.UserIdOrDefault ?? throw new InvalidOperationException("A stream needs a signed-in subscriber.");

		// Subscribed before the first await, which runs before the endpoint opens the response, so nothing published
		// while MatchesAsync loads is lost
		using EventSubscription<TSignal> subscription = streams.Subscribe(subscriber, user.AccessTokenExpiry);
		Func<TSignal, bool> matches = await MatchesAsync(query, subscriber, cancellationToken);

		await foreach (TSignal signal in subscription.ReadAllAsync(cancellationToken))
		{
			if (matches(signal))
			{
				yield return signal;
			}
		}
	}

	// Runs once when the stream opens and may query. The filter it returns runs for each signal in the same request,
	// and a change to what this method read closes the stream through SignalBackplane.CloseStreamsFor
	protected virtual ValueTask<Func<TSignal, bool>> MatchesAsync(TQuery query, UserId subscriber, CancellationToken cancellationToken) =>
		ValueTask.FromResult<Func<TSignal, bool>>(static _ => true);
}
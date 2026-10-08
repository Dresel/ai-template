using System.Globalization;
using System.Security.Claims;
using FocusTemplate.Admin.Api.Streams;
using FocusTemplate.Admin.Shared.UserManagement;
using Mediator;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

public sealed class GroupsWatchHandler(SignalHub<GroupChanged> hub, IHttpContextAccessor httpContextAccessor)
	: IStreamQueryHandler<GroupsWatchQuery, GroupChanged>
{
	// Whoever may view groups sees every group. The permission is checked at connect only, so the stream ends with the
	// access token, and the reconnect checks it again
	public IAsyncEnumerable<GroupChanged> Handle(GroupsWatchQuery query, CancellationToken cancellationToken) =>
		hub.WatchAsync(_ => true, ExpiryOf(httpContextAccessor.HttpContext?.User), cancellationToken);

	private static DateTimeOffset? ExpiryOf(ClaimsPrincipal? user) =>
		user?.FindFirstValue(JwtRegisteredClaimNames.Exp) is { } seconds
			? DateTimeOffset.FromUnixTimeSeconds(long.Parse(seconds, CultureInfo.InvariantCulture))
			: null;
}
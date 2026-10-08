using FocusTemplate.Admin.Api.Streams;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Primitives;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

// By hand until the emitter generates server-sent event operations. Mapped with its full template,
// since the /groups route group would put a slash before the colon
public static class GroupsWatchEndpoint
{
	public static RouteHandlerBuilder MapGroupsWatchEndpoint(this IEndpointRouteBuilder app) =>
		app.MapGet("/groups:watch", Watch)
			.WithName("UserManagement.Groups_watch")
			.WithTags("Groups")
			.Produces<GroupChanged>(200, "text/event-stream")
			.Produces(401)
			.Produces(403)
			.RequireAuthorization()
			.WithMetadata(new RequiresPermissionAttribute(UserManagementPermissions.ViewGroups));

	private static ServerSentEventsResult<GroupChanged?> Watch(IMediator mediator, HttpResponse response, CancellationToken cancellationToken)
	{
		// nginx passes each event on as it comes instead of buffering the response
		response.Headers["X-Accel-Buffering"] = "no";

		return TypedResults.ServerSentEvents(
			ServerSentEventStreams.WithKeepAlive(mediator.CreateStream(new GroupsWatchQuery(), cancellationToken), "groupChanged", cancellationToken));
	}
}
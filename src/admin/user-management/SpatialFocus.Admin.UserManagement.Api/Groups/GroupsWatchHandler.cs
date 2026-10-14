using Microsoft.AspNetCore.Http;
using SpatialFocus.Admin.UserManagement.Shared;
using SpatialFocus.Api.Foundation.Realtime;

namespace SpatialFocus.Admin.UserManagement.Api.Groups;

public sealed class GroupsWatchHandler(EventStreams<GroupChanged> streams, IHttpContextAccessor httpContextAccessor)
	: EventStreamHandler<GroupsWatchQuery, GroupChanged>(streams, httpContextAccessor);
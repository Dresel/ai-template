using FocusTemplate.Admin.UserManagement.Shared;
using FocusTemplate.Api.Foundation.Realtime;
using Microsoft.AspNetCore.Http;

namespace FocusTemplate.Admin.UserManagement.Api.Groups;

public sealed class GroupsWatchHandler(EventStreams<GroupChanged> streams, IHttpContextAccessor httpContextAccessor)
	: EventStreamHandler<GroupsWatchQuery, GroupChanged>(streams, httpContextAccessor);
using FocusTemplate.Admin.Api.Realtime;
using FocusTemplate.Admin.Shared.UserManagement;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

public sealed class GroupsWatchHandler(EventStreams<GroupChanged> streams, IHttpContextAccessor httpContextAccessor)
	: EventStreamHandler<GroupsWatchQuery, GroupChanged>(streams, httpContextAccessor);
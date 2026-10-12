using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Api.Foundation.Realtime;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

public sealed class GroupsWatchHandler(EventStreams<GroupChanged> streams, IHttpContextAccessor httpContextAccessor)
	: EventStreamHandler<GroupsWatchQuery, GroupChanged>(streams, httpContextAccessor);
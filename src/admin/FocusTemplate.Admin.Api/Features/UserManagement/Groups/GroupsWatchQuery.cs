using FocusTemplate.Admin.Shared.UserManagement;
using Mediator;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

// By hand until the emitter generates server-sent event operations
public sealed record GroupsWatchQuery : IStreamQuery<GroupChanged>;
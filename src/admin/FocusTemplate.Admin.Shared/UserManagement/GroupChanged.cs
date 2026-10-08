using FocusTemplate.Primitives;

namespace FocusTemplate.Admin.Shared.UserManagement;

// The event of GET /groups:watch, written by hand until the emitter generates server-sent events.
// A signal only: the client reads the group again
public sealed record GroupChanged(GroupId Id, GroupChange Change);
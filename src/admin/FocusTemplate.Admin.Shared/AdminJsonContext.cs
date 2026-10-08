using System.Text.Json.Serialization;
using FocusTemplate.Admin.Shared.UserManagement;

namespace FocusTemplate.Admin.Shared;

// The server-sent events written by hand, until the emitter generates them into the generated half
[JsonSerializable(typeof(GroupChanged))]
public sealed partial class AdminJsonContext;
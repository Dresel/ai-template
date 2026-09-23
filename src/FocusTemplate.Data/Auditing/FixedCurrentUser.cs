using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Auditing;

public sealed record FixedCurrentUser(UserId Id) : ICurrentUser;
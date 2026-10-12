using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Foundation.Auditing;

public sealed record FixedCurrentUser(UserId Id) : ICurrentUser
{
	public UserId? IdOrDefault => Id;
}
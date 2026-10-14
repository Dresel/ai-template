using SpatialFocus.Primitives;

namespace SpatialFocus.Data.Foundation.Auditing;

public sealed record FixedCurrentUser(UserId Id) : ICurrentUser
{
	public UserId? IdOrDefault => Id;
}
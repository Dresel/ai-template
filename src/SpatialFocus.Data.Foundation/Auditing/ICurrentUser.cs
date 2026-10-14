using SpatialFocus.Primitives;

namespace SpatialFocus.Data.Foundation.Auditing;

public interface ICurrentUser
{
	public UserId Id => IdOrDefault ?? throw new InvalidOperationException("User not signed in.");

	public UserId? IdOrDefault { get; }
}
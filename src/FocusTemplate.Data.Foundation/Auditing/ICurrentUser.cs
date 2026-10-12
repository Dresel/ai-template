using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Foundation.Auditing;

public interface ICurrentUser
{
	public UserId Id => IdOrDefault ?? throw new InvalidOperationException("User not signed in.");

	public UserId? IdOrDefault { get; }
}
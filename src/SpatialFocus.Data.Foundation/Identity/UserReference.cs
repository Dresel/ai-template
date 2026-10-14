using SpatialFocus.Primitives;

namespace SpatialFocus.Data.Foundation.Identity;

// The spine's users as a feature's context sees them: the key a foreign key points at and what a page shows of a user.
// Navigations read it, and nothing writes it. UserReferenceTests keeps it in step with the spine's User.
public sealed class UserReference
{
	public required UserId Id { get; init; }

	public required string DisplayName { get; init; }

	public bool IsActive { get; init; }
}
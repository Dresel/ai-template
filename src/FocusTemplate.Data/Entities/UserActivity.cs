using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Entities;

// Outsourced to avoid IAuditable.
public sealed class UserActivity
{
	public required UserId UserId { get; init; }

	public required DateTimeOffset LastSeenAt { get; set; }
}
using System.ComponentModel.DataAnnotations;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Entities;

public sealed class User : IAuditable
{
	public const int DisplayNameLength = 200;

	public required UserId Id { get; init; }

	[MaxLength(DisplayNameLength)]
	public required string DisplayName { get; set; }

	// 64 before the @, 255 after it (RFC 5321), wider than anything Keycloak stores
	[MaxLength(320)]
	public string? Email { get; set; }

	public required DateTimeOffset FirstSeenAt { get; init; }

	public bool IsActive { get; set; } = true;

	public UserActivity? Activity { get; set; }

	public List<Group> Groups { get; } = [];
}
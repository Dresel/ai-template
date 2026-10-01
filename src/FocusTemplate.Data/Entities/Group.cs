using System.ComponentModel.DataAnnotations;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Entities;

public sealed class Group : IAuditable
{
	public required GroupId Id { get; init; }

	[MaxLength(100)]
	public required string Name { get; set; }

	[MaxLength(500)]
	public string? Description { get; set; }

	public bool IsManaged { get; init; }

	public List<User> Members { get; } = [];

	public List<PermissionDefinition> Permissions { get; } = [];
}
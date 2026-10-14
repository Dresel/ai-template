using System.ComponentModel.DataAnnotations;
using SpatialFocus.Data.Foundation.Auditing;
using SpatialFocus.Primitives;

namespace SpatialFocus.Data.Entities;

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
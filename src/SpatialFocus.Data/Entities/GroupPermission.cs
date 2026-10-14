using System.ComponentModel.DataAnnotations;
using SpatialFocus.Data.Foundation.Auditing;
using SpatialFocus.Primitives;

namespace SpatialFocus.Data.Entities;

public sealed class GroupPermission : IAuditable
{
	public required GroupId GroupId { get; init; }

	[MaxLength(PermissionDefinition.NameLength)]
	public required Permission Permission { get; init; }
}
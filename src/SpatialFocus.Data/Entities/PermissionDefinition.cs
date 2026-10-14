using System.ComponentModel.DataAnnotations;
using SpatialFocus.Primitives;

namespace SpatialFocus.Data.Entities;

public sealed class PermissionDefinition
{
	public const int NameLength = 100;

	[MaxLength(NameLength)]
	public required Permission Name { get; init; }
}
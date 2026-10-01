using System.ComponentModel.DataAnnotations;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Entities;

public sealed class GroupPermission : IAuditable
{
	public required GroupId GroupId { get; init; }

	[MaxLength(PermissionDefinition.NameLength)]
	public required Permission Permission { get; init; }
}
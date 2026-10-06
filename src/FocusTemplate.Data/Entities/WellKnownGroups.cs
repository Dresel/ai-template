using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Entities;

// Managed system groups seeded via migration.
public static class WellKnownGroups
{
	public static GroupId Administrators { get; } = GroupId.From(new Guid("00000000-0000-7000-8001-000000000001"));
}
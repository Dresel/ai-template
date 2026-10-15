using SpatialFocus.Primitives;
using SpatialFocus.Weather.Primitives;

namespace SpatialFocus.Testing.Host;

// The permission sets of the packages the host includes, as a product's AdminPermissions lists its own. A package that
// gains a permission adds it here, or the grants of its tests fail on the missing row.
public static class TestHostPermissions
{
	public static IReadOnlyList<Permission> All { get; } =
	[
		UserManagementPermissions.ViewUsers,
		UserManagementPermissions.ManageUsers,
		UserManagementPermissions.ViewGroups,
		UserManagementPermissions.ManageGroups,
		WeatherPermissions.ManageStations,
	];
}
using SpatialFocus.Primitives;

namespace SpatialFocus.Admin.UserManagement.Shared;

// The permissions a group can be granted: the product's, which the host hands over, since only the product knows which
// features it includes
public sealed record PermissionCatalog(IReadOnlyList<Permission> All);
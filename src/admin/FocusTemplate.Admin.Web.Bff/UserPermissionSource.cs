using SpatialFocus.Admin.UserManagement.Client.Users;
using SpatialFocus.Primitives;
using SpatialFocus.Web.Bff.Hosting;

namespace FocusTemplate.Admin.Web.Bff;

// User management's /users/me, the source of the permissions every Admin product's BFF hands its client
internal sealed class UserPermissionSource(UsersClient users) : IUserPermissionSource
{
	public async Task<IReadOnlyList<Permission>> GetAsync(CancellationToken cancellationToken) =>
		(await users.MeAsync(cancellationToken)).Permissions;
}
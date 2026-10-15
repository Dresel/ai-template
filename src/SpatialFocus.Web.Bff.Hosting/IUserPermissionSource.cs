using SpatialFocus.Primitives;

namespace SpatialFocus.Web.Bff.Hosting;

// The permissions the product's API grants the signed-in user, which /bff/user adds to the session's claims. The
// product's BFF registers it, since only the product knows which API answers.
public interface IUserPermissionSource
{
	public Task<IReadOnlyList<Permission>> GetAsync(CancellationToken cancellationToken);
}
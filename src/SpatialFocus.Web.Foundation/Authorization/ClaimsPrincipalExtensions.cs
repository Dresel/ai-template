using System.Security.Claims;
using SpatialFocus.Primitives;

namespace SpatialFocus.Web.Foundation.Authorization;

public static class ClaimsPrincipalExtensions
{
	extension(ClaimsPrincipal user)
	{
		// The permissions the BFF added to /bff/user from the API's answer
		public bool Has(Permission permission) => user.HasClaim(Permission.ClaimType, permission.Value);
	}
}
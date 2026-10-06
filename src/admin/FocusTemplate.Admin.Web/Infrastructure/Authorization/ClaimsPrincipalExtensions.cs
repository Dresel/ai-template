using System.Security.Claims;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Primitives;

namespace FocusTemplate.Admin.Web.Infrastructure.Authorization;

public static class ClaimsPrincipalExtensions
{
	extension(ClaimsPrincipal user)
	{
		// The permissions the BFF added to /bff/user from the API's answer
		public bool Has(Permission permission) => user.HasClaim(BffClaimTypes.Permission, permission.Value);
	}
}
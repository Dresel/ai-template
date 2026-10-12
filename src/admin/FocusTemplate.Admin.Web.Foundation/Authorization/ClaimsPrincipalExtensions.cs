using System.Security.Claims;
using FocusTemplate.Primitives;

namespace FocusTemplate.Admin.Web.Foundation.Authorization;

public static class ClaimsPrincipalExtensions
{
	extension(ClaimsPrincipal user)
	{
		// The permissions the BFF added to /bff/user from the API's answer
		public bool Has(Permission permission) => user.HasClaim(Permission.ClaimType, permission.Value);
	}
}
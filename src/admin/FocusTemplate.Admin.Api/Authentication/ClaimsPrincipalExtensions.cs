using System.Security.Claims;
using FocusTemplate.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FocusTemplate.Admin.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
	// The subject is Keycloak's user UUID and therefore the UserId; null when nobody is signed in.
	public static UserId? GetUserId(this ClaimsPrincipal principal) =>
		principal.Identity?.IsAuthenticated == true &&
		Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out Guid id)
			? UserId.From(id)
			: null;
}
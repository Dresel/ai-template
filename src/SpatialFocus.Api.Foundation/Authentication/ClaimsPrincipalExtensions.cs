using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using SpatialFocus.Primitives;

namespace SpatialFocus.Api.Foundation.Authentication;

public static class ClaimsPrincipalExtensions
{
	extension(ClaimsPrincipal principal)
	{
		public DateTimeOffset? AccessTokenExpiry =>
			long.TryParse(
				principal.FindFirstValue(JwtRegisteredClaimNames.Exp),
				NumberStyles.Integer,
				CultureInfo.InvariantCulture,
				out long seconds)
				? DateTimeOffset.FromUnixTimeSeconds(seconds)
				: null;

		public UserId? UserIdOrDefault =>
			principal.Identity?.IsAuthenticated == true && Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out Guid id)
				? UserId.From(id)
				: null;
	}
}
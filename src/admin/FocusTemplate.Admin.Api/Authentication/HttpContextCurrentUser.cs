using System.Security.Claims;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FocusTemplate.Admin.Api.Authentication;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
	public UserId Id
	{
		get
		{
			ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;

			if (user?.Identity?.IsAuthenticated != true)
			{
				return WellKnownUsers.System;
			}

			string? subject = user.FindFirstValue(JwtRegisteredClaimNames.Sub);

			return Guid.TryParse(subject, out Guid id)
				? UserId.From(id)
				: throw new InvalidOperationException("The authenticated user carries no UUID sub claim.");
		}
	}
}
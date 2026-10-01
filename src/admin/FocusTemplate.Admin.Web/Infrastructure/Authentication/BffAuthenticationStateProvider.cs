using System.Security.Claims;
using FocusTemplate.Admin.Shared;
using Microsoft.AspNetCore.Components.Authorization;

namespace FocusTemplate.Admin.Web.Infrastructure.Authentication;

public sealed class BffAuthenticationStateProvider(AuthenticationClient client) : AuthenticationStateProvider
{
	private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

	public override async Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		UserInfoResponse? user = await client.GetUserAsync();

		if (user is null)
		{
			return Anonymous;
		}

		ClaimsIdentity identity = new(
			user.Claims.Select(claim => new Claim(claim.Type, claim.Value)),
			"bff",
			"preferred_username",
			ClaimTypes.Role);

		return new AuthenticationState(new ClaimsPrincipal(identity));
	}
}
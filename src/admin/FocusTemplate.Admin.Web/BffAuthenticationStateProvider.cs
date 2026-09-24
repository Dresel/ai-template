using System.Security.Claims;
using FocusTemplate.Admin.Shared;
using Microsoft.AspNetCore.Components.Authorization;

namespace FocusTemplate.Admin.Web;

// The session is the BFF's cookie, so the client learns who is signed in by asking the BFF, once per load.
public sealed class BffAuthenticationStateProvider(AuthenticationClient client) : AuthenticationStateProvider
{
	private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

	public override async Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		UserInfoResponse? user;

		try
		{
			user = await client.GetUserAsync();
		}
		catch (HttpRequestException)
		{
			return Anonymous;
		}

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
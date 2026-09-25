using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FocusTemplate.Admin.Web.Bff;

internal static class HttpContextExtensions
{
	// A token that cannot be refreshed means the Keycloak session is gone, so the BFF session ends too.
	public static async Task<string?> GetAccessTokenOrSignOutAsync(
		this HttpContext context,
		CancellationToken cancellationToken)
	{
		TokenResult<UserToken> token = await context.GetUserAccessTokenAsync(ct: cancellationToken);

		if (token is { Token: not null, Succeeded: true, })
		{
			return token.Token.AccessToken;
		}

		await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

		return null;
	}
}
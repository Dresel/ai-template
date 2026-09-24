using System.Security.Claims;
using FocusTemplate.Admin.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FocusTemplate.Admin.Web.Bff;

internal static class AuthenticationEndpoints
{
	public static WebApplication MapAuthenticationEndpoints(this WebApplication app)
	{
		// The WASM client sends the browser here, Keycloak sends it back to the page it came from.
		app.MapGet(
			"/bff/login",
			(string? returnUrl) => Results.Challenge(
				new AuthenticationProperties { RedirectUri = LocalPathOrRoot(returnUrl), },
				[OpenIdConnectDefaults.AuthenticationScheme,]));

		// A GET, so a top-level navigation can carry the browser on to Keycloak's end-session page. The session id in the
		// query is its CSRF protection: only the page holding the session learned it from /bff/user.
		app.MapGet(
			"/bff/logout",
			(string? sid, string? returnUrl, ClaimsPrincipal user) => user.Identity?.IsAuthenticated switch
			{
				not true => Results.Redirect(LocalPathOrRoot(returnUrl)),
				_ when sid is null || sid != user.FindFirstValue(JwtRegisteredClaimNames.Sid) => Results.BadRequest(),
				_ => Results.SignOut(
					new AuthenticationProperties { RedirectUri = LocalPathOrRoot(returnUrl), },
					[CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme,]),
			});

		// 401 rather than a challenge: the client asks on every load and reads 401 as "anonymous".
		app.MapGet(
			"/bff/user",
			(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true
				? Results.Ok(
					new UserInfoResponse(
					[
						.. user.Claims.Select(claim => new UserClaim(claim.Type, claim.Value)),
						new UserClaim(BffClaimTypes.LogoutUrl, LogoutUrl(user)),
					]))
				: Results.Unauthorized());

		return app;
	}

	// Prevent open redirect attacks. IsLocalUrl alone also passes "~/" paths, which the OIDC handlers redirect to verbatim.
	private static string LocalPathOrRoot(string? returnUrl) =>
		returnUrl is ['/', ..,] && RedirectHttpResult.IsLocalUrl(returnUrl) ? returnUrl : "/";

	private static string LogoutUrl(ClaimsPrincipal user) =>
		$"/bff/logout?sid={Uri.EscapeDataString(user.FindFirstValue(JwtRegisteredClaimNames.Sid) ?? string.Empty)}";
}
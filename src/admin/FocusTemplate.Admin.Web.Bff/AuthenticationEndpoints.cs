using System.Security.Claims;
using FocusTemplate.Admin.Client.Users;
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

		// 401 rather than a challenge: the client asks on every load and reads 401 as "anonymous". A session whose token
		// can no longer be refreshed ends here already. The permissions come from the API. When it refuses the token or
		// fails, 502 as on the proxied routes, since the session is fine and a new login would bring the same token.
		app.MapGet(
			"/bff/user",
			async (HttpContext context, UsersClient users) =>
			{
				context.Response.Headers.CacheControl = "no-store";

				if (context.User.Identity?.IsAuthenticated != true ||
					await context.GetAccessTokenOrSignOutAsync(context.RequestAborted) is null)
				{
					return Results.Unauthorized();
				}

				try
				{
					CurrentUserResponse me = await users.MeAsync(context.RequestAborted);

					return Results.Ok(
						new UserInfoResponse(
						[
							.. context.User.Claims.Select(claim => new UserClaim(claim.Type, claim.Value)),
							new UserClaim(BffClaimTypes.LogoutUrl, LogoutUrl(context.User)),
							.. me.Permissions.Select(permission => new UserClaim(BffClaimTypes.Permission, permission.Value)),
						]));
				}
				catch (HttpRequestException)
				{
					return Results.StatusCode(StatusCodes.Status502BadGateway);
				}
			});

		return app;
	}

	// Prevent open redirect attacks. IsLocalUrl alone also passes "~/" paths, which the OIDC handlers redirect to verbatim.
	private static string LocalPathOrRoot(string? returnUrl) =>
		returnUrl is ['/', ..,] && RedirectHttpResult.IsLocalUrl(returnUrl) ? returnUrl : "/";

	private static string LogoutUrl(ClaimsPrincipal user) =>
		$"/bff/logout?sid={Uri.EscapeDataString(user.FindFirstValue(JwtRegisteredClaimNames.Sid) ?? string.Empty)}";
}
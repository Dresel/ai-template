using System.Net.Http.Headers;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;

namespace FocusTemplate.Admin.Web.Bff;

internal static class ReverseProxyBuilderExtensions
{
	// The browser sends the BFF's cookies, the session ticket among them, on every same-origin request. No destination
	// needs them, and none may set cookies on the BFF's origin.
	public static IReverseProxyBuilder AddCookieIsolationTransform(this IReverseProxyBuilder proxy) =>
		proxy.AddTransforms(context =>
		{
			context.AddRequestHeaderRemove(HeaderNames.Cookie);

			// Only the destination's values: when the token management refreshed a token for this request, the response
			// already carries the BFF's renewed session cookie, which has to reach the browser.
			context.AddResponseTransform(transform =>
			{
				if (transform.ProxyResponse is { } response &&
					response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string>? fromDestination))
				{
					IHeaderDictionary headers = transform.HttpContext.Response.Headers;
					headers.SetCookie = [with([.. headers.SetCookie.Except(fromDestination),]),];
				}

				return ValueTask.CompletedTask;
			});
		});

	// Only routes under the ProxiedApi policy carry a signed-in user. A token the management cannot refresh means the
	// session at Keycloak is gone, so the BFF session ends with it: /bff/user answers 401 again and the client goes back
	// through login. A status other than 200 makes YARP answer without forwarding.
	public static IReverseProxyBuilder AddAccessTokenTransform(this IReverseProxyBuilder proxy) =>
		proxy.AddTransforms(context =>
		{
			if (!string.Equals(context.Route.AuthorizationPolicy, ProxiedApiDefaults.Policy, StringComparison.OrdinalIgnoreCase))
			{
				// The BFF attaches no token here, and a caller's own must not pass either.
				context.AddRequestHeaderRemove(HeaderNames.Authorization);
				return;
			}

			context.AddRequestTransform(async transform =>
			{
				TokenResult<UserToken> token =
					await transform.HttpContext.GetUserAccessTokenAsync(ct: transform.CancellationToken);

				if (token is { Token: not null, Succeeded: true, })
				{
					string accessToken = token.Token.AccessToken;
					transform.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
					return;
				}

				await transform.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
				transform.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
			});
		});
}
using System.Net.Http.Headers;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Yarp.ReverseProxy.Transforms;

namespace FocusTemplate.Admin.Web.Bff;

internal static class ReverseProxyBuilderExtensions
{
	// Only routes under the ProxiedApi policy carry a signed-in user. A token the management cannot refresh means the
	// session at Keycloak is gone, and the request then goes out without one: the API answers the 401 the client needs,
	// so nothing has to short-circuit ahead of the forwarder.
	public static IReverseProxyBuilder AddAccessTokenTransform(this IReverseProxyBuilder proxy) =>
		proxy.AddTransforms(context =>
		{
			if (context.Route.AuthorizationPolicy != ProxiedApiDefaults.Policy)
			{
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
				}
			});
		});
}
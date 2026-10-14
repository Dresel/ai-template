using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SpatialFocus.Web.Bff.Hosting;

public static class ServiceCollectionExtensions
{
	// A typed client of the product's API, called with the session's access token
	public static IHttpClientBuilder AddApiClient<TClient>(this IServiceCollection services)
		where TClient : class =>
		services.AddHttpClient<TClient>((provider, client) =>
				client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<BffOptions>>().Value.Api))
			.AddUserAccessTokenHandler();
}
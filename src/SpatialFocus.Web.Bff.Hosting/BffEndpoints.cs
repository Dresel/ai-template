using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SpatialFocus.Web.Bff.Hosting;

public static class BffEndpoints
{
	public static WebApplication MapBffEndpoints(this WebApplication app)
	{
		// The AppHost sets Client__ConfigEndpointPath and Client__ConfigResponse, and the BFF serves that JSON to the WASM client.
		string? configEndpointPath = app.Configuration["Client:ConfigEndpointPath"];
		string? configResponse = app.Configuration["Client:ConfigResponse"];

		if (!string.IsNullOrEmpty(configEndpointPath) && !string.IsNullOrEmpty(configResponse))
		{
			app.MapGet(configEndpointPath, () => Results.Content(configResponse, "application/json"));
		}

		app.MapGet("/client-configuration", (IOptions<ClientConfiguration> config) => config.Value);

		app.MapAuthenticationEndpoints();

		app.MapReverseProxy();

		return app;
	}
}
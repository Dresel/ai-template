using FocusTemplate.Api.Foundation.Realtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FocusTemplate.Api.Hosting;

public static class WebApplicationBuilderExtensions
{
	// What every API sets up, a public one included
	public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
	{
		builder.Services.AddSingleton(TimeProvider.System);

		builder.Services.AddProblemDetails();

		builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.RespectRequiredConstructorParameters = true);

		return builder;
	}

	// For a host with event streams: Redis carries their signals between its processes
	public static WebApplicationBuilder AddSignalBackplane(this WebApplicationBuilder builder, string connectionName)
	{
		builder.AddRedisClient(connectionName);
		builder.Services.AddSignalBackplane();

		return builder;
	}
}
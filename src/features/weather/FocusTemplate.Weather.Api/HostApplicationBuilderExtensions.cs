using FocusTemplate.Weather.Data;
using FocusTemplate.Weather.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResXGenerator.Registration;

namespace FocusTemplate.Weather.Api;

public static class HostApplicationBuilderExtensions
{
	public static IHostApplicationBuilder AddWeather(
		this IHostApplicationBuilder builder,
		string connectionName,
		string readOnlyConnectionName)
	{
		builder.Services.AddWeatherDbContextPool(connectionName);
		builder.EnrichNpgsqlDbContext<WeatherDbContext>();

		builder.Services.AddReadOnlyWeatherDbContextPool(readOnlyConnectionName);
		builder.EnrichNpgsqlDbContext<ReadOnlyWeatherDbContext>();

		builder.Services.ConfigureHttpJsonOptions(options =>
			options.SerializerOptions.TypeInfoResolverChain.Insert(0, WeatherJsonContext.Default));
		builder.Services.AddWeatherValidators();
		builder.Services.AddHttpContextAccessor();

		builder.Services.UsingResXGenerator();

		return builder;
	}
}
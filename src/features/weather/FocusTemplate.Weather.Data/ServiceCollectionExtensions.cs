using FocusTemplate.Data.Foundation;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTemplate.Weather.Data;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddReadOnlyWeatherDbContextPool(this IServiceCollection services, string connectionName) =>
		services.AddReadOnlyFeatureDbContextPool<ReadOnlyWeatherDbContext>(connectionName, npgsql => npgsql.UseWeatherTypes());

	public static IServiceCollection AddWeatherDbContextPool(this IServiceCollection services, string connectionName) =>
		services.AddFeatureDbContextPool<WeatherDbContext>(connectionName, npgsql => npgsql.UseWeatherTypes());
}
using Microsoft.EntityFrameworkCore;
using SpatialFocus.Data.Foundation;
using SpatialFocus.Data.Foundation.Auditing;

namespace SpatialFocus.Weather.Data;

public static class DbContextOptionsBuilderExtensions
{
	public static DbContextOptionsBuilder<ReadOnlyWeatherDbContext> ConfigureReadOnlyWeatherDbContext(
		this DbContextOptionsBuilder<ReadOnlyWeatherDbContext> options,
		string connectionString) =>
		options.ConfigureReadOnlyFeatureDbContext(connectionString, npgsql => npgsql.UseWeatherTypes());

	public static DbContextOptionsBuilder<WeatherDbContext> ConfigureWeatherDbContext(
		this DbContextOptionsBuilder<WeatherDbContext> options,
		string connectionString,
		AuditingInterceptor auditing) =>
		options.ConfigureFeatureDbContext(connectionString, auditing, npgsql => npgsql.UseWeatherTypes());
}
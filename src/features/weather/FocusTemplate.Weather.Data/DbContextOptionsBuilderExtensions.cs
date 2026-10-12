using FocusTemplate.Data.Foundation;
using FocusTemplate.Data.Foundation.Auditing;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Weather.Data;

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
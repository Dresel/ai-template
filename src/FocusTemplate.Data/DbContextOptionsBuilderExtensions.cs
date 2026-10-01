using FocusTemplate.Data.Auditing;
using FocusTemplate.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

public static class DbContextOptionsBuilderExtensions
{
	public static TBuilder ConfigureAppDbContext<TBuilder>(
		this TBuilder options,
		string connectionString,
		AuditingInterceptor auditing)
		where TBuilder : DbContextOptionsBuilder
	{
		options.UseAppDbContextProvider(connectionString);
		options.AddInterceptors(auditing);

		return options;
	}

	public static TBuilder ConfigureReadOnlyAppDbContext<TBuilder>(this TBuilder options, string connectionString)
		where TBuilder : DbContextOptionsBuilder
	{
		options.UseAppDbContextProvider(connectionString);

		// No auditing interceptor and no tracking: this context never saves, so neither would have anything to do.
		options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

		return options;
	}

	private static void UseAppDbContextProvider(this DbContextOptionsBuilder options, string connectionString)
	{
		options.UseNpgsql(
			connectionString,
			npgsql => npgsql.UseNetTopologySuite()
				.MapEnum<StationStatus>("station_status")
				.MapEnum<AlertKind>("alert_kind"));

		options.UseSnakeCaseNamingConvention();
		options.UseValidationCheckConstraints();
	}
}
using Microsoft.EntityFrameworkCore;
using SpatialFocus.Data.Foundation;
using SpatialFocus.Data.Foundation.Auditing;

namespace SpatialFocus.Data;

public static class DbContextOptionsBuilderExtensions
{
	public static TBuilder ConfigureAppDbContext<TBuilder>(this TBuilder options, string connectionString, AuditingInterceptor auditing)
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

	private static void UseAppDbContextProvider(this DbContextOptionsBuilder options, string connectionString) =>
		options.UseNpgsqlWithConventions(connectionString);
}
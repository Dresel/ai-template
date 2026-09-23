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
		options.UseNpgsql(
			connectionString,
			npgsql => npgsql.UseNetTopologySuite()
				.MapEnum<StationStatus>("station_status")
				.MapEnum<AlertKind>("alert_kind"));

		options.UseSnakeCaseNamingConvention();
		options.UseValidationCheckConstraints();

		options.AddInterceptors(auditing);

		return options;
	}
}
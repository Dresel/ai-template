using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using SpatialFocus.Weather.Data.Entities;

namespace SpatialFocus.Weather.Data;

public static class NpgsqlDbContextOptionsBuilderExtensions
{
	public static void UseWeatherTypes(this NpgsqlDbContextOptionsBuilder npgsql) =>
		npgsql.UseNetTopologySuite()
			.MapEnum<StationStatus>(schemaName: WeatherDbContextBase.Schema)
			.MapEnum<AlertKind>(schemaName: WeatherDbContextBase.Schema);
}
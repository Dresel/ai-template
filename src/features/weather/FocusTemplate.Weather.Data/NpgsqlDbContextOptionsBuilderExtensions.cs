using FocusTemplate.Weather.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace FocusTemplate.Weather.Data;

public static class NpgsqlDbContextOptionsBuilderExtensions
{
	public static void UseWeatherTypes(this NpgsqlDbContextOptionsBuilder npgsql) =>
		npgsql.UseNetTopologySuite()
			.MapEnum<StationStatus>(schemaName: WeatherDbContextBase.Schema)
			.MapEnum<AlertKind>(schemaName: WeatherDbContextBase.Schema);
}
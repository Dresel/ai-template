using FocusTemplate.Data.Auditing;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Npgsql;

namespace FocusTemplate.Data;

public static class WeatherSeed
{
	public static IReadOnlyList<(int TemperatureC, string Summary)> Values { get; } =
	[
		(18, "Mild"), (21, "Warm"), (24, "Warm"), (28, "Hot"), (31, "Scorching"),
	];

	// See https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding#configuration-options-useseeding-and-useasyncseeding-methods
	public static DbContextOptionsBuilder UseWeatherSeeding(this DbContextOptionsBuilder optionsBuilder) =>
		optionsBuilder.UseSeeding((context, _) =>
			{
				if (Seed(context))
				{
					context.SaveChanges();
				}
			})
			.UseAsyncSeeding(async (context, _, cancellationToken) =>
			{
				if (Seed(context))
				{
					await context.SaveChangesAsync(cancellationToken);
				}
			});

	// The seed runs on the migration connection, whose Npgsql type catalog predates PostGIS.
	// Reloading is required so the first geography insert can resolve the type.
	private static void ReloadTypes(DbContext context)
	{
		if (context.Database.GetDbConnection() is NpgsqlConnection connection)
		{
			context.Database.OpenConnection();
			connection.ReloadTypes();
		}
	}

	// Shared by both seed delegates - the EF CLI calls the synchronous one, so neither may be left out.
	private static bool Seed(DbContext context)
	{
		if (context.Set<Station>().Any())
		{
			return false;
		}

		ReloadTypes(context);

		DateOnly today = DateOnly.FromDateTime(DateTime.Now);
		DateTimeOffset now = DateTimeOffset.UtcNow;

		Station vienna = new()
		{
			Id = StationId.New(),
			OwnerId = WellKnownUsers.Developer,
			Code = "VIE",
			Name = "Vienna Danube Island",
			Description = "Reference station on the Danube Island, 1 m above the meadow.",
			Location = new Point(16.4108, 48.2263) { SRID = 4326, },
			MinTemperatureC = -15,
			MaxTemperatureC = 35,
		};
		Station graz = new()
		{
			Id = StationId.New(),
			OwnerId = WellKnownUsers.Developer,
			Code = "GRZ",
			Name = "Graz Schlossberg",
			Location = new Point(15.4374, 47.0755) { SRID = 4326, },
		};

		context.Set<Station>().AddRange(vienna, graz);

		context.Set<WeatherForecast>()
			.AddRange(
				Values.Select((value, index) => new WeatherForecast
				{
					StationId = vienna.Id,
					Date = today.AddDays(index + 1),
					TemperatureC = value.TemperatureC,
					Summary = value.Summary,
				}));

		context.Set<Observation>()
			.AddRange(
				new Observation
				{
					StationId = vienna.Id,
					MeasuredAt = now.AddHours(-2),
					TemperatureC = 17.4,
					HumidityPercent = 61,
					PressureHpa = 1013.2,
				},
				new Observation
				{
					StationId = vienna.Id,
					MeasuredAt = now.AddHours(-1),
					TemperatureC = 18.1,
					HumidityPercent = 58,
					PressureHpa = 1012.9,
				},
				new Observation
				{
					StationId = graz.Id,
					MeasuredAt = now.AddHours(-1),
					TemperatureC = 16.2,
					HumidityPercent = 66,
					PressureHpa = 1009.4,
				});

		return true;
	}
}
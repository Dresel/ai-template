using Argus.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Argus.Data;

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
				if (CreateMissingForecasts(context) is { } forecasts)
				{
					context.Set<WeatherForecast>().AddRange(forecasts);
					context.SaveChanges();
				}
			})
			.UseAsyncSeeding(async (context, _, cancellationToken) =>
			{
				if (CreateMissingForecasts(context) is { } forecasts)
				{
					context.Set<WeatherForecast>().AddRange(forecasts);
					await context.SaveChangesAsync(cancellationToken);
				}
			});

	private static List<WeatherForecast>? CreateMissingForecasts(DbContext context)
	{
		if (context.Set<WeatherForecast>().Any())
		{
			return null;
		}

		DateOnly today = DateOnly.FromDateTime(DateTime.Now);

		return
		[
			..Values.Select((value, index) => new WeatherForecast
			{
				Date = today.AddDays(index + 1), TemperatureC = value.TemperatureC, Summary = value.Summary,
			}),
		];
	}
}
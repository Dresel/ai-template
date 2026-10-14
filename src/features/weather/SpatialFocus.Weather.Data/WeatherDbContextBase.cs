using Microsoft.EntityFrameworkCore;
using SpatialFocus.Data.Foundation;
using SpatialFocus.Primitives;
using SpatialFocus.Weather.Data.Entities;
using SpatialFocus.Weather.Primitives;

namespace SpatialFocus.Weather.Data;

public abstract class WeatherDbContextBase(DbContextOptions options) : FoundationDbContext<WeatherDbContextBase>(options), IHasSchema
{
	public static string Schema => "weather";

	public DbSet<Alert> Alerts => Set<Alert>();

	public DbSet<Observation> Observations => Set<Observation>();

	public DbSet<Station> Stations => Set<Station>();

	public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();

	protected sealed override void ConfigureIds(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder.RegisterAllInWeatherEfCoreConverters();

		configurationBuilder.Properties<StationId>().HaveSentinel(StationId.Unspecified);
		configurationBuilder.Properties<ObservationId>().HaveSentinel(ObservationId.Unspecified);
		configurationBuilder.Properties<WeatherForecastId>().HaveSentinel(WeatherForecastId.Unspecified);
		configurationBuilder.Properties<AlertId>().HaveSentinel(AlertId.Unspecified);
	}

	protected sealed override void ConfigureModel(ModelBuilder modelBuilder)
	{
		// The migration creates the extension, which the postgis/postgis image ships.
		modelBuilder.HasPostgresExtension("postgis");

		modelBuilder.ApplyWeatherConfigurations();
	}
}
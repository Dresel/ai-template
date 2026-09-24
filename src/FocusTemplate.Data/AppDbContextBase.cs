using FocusTemplate.Data.Auditing;
using FocusTemplate.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

// Base class for writable and read-only db contexts.
public abstract class AppDbContextBase(DbContextOptions options) : DbContext(options)
{
	public DbSet<Station> Stations => Set<Station>();

	public DbSet<Observation> Observations => Set<Observation>();

	public DbSet<Alert> Alerts => Set<Alert>();

	public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();

	protected sealed override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
		configurationBuilder.RegisterAllInVogenEfCoreConverters();

	protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// The migration creates the extension; the postgis/postgis image ships it
		modelBuilder.HasPostgresExtension("postgis");

		modelBuilder.ApplyEntityConfigurations();
		modelBuilder.AddAuditingShadowProperties();
	}
}
using FocusTemplate.Data.Auditing;
using FocusTemplate.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<Station> Stations => Set<Station>();

	public DbSet<Observation> Observations => Set<Observation>();

	public DbSet<Alert> Alerts => Set<Alert>();

	public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
		configurationBuilder.RegisterAllInVogenEfCoreConverters();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// The migration creates the extension; the postgis/postgis image ships it
		modelBuilder.HasPostgresExtension("postgis");

		modelBuilder.ApplyEntityConfigurations();
		modelBuilder.AddAuditingShadowProperties();
	}
}
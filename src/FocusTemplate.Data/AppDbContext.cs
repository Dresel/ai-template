using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
		configurationBuilder.RegisterAllInVogenEfCoreConverters();

	protected override void OnModelCreating(ModelBuilder modelBuilder) =>

		modelBuilder.Entity<WeatherForecast>()
			.Property(forecast => forecast.Id)
			.ValueGeneratedOnAdd()
			.HasSentinel(WeatherForecastId.Unspecified);
}
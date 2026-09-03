using FocusTemplate.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<ChainCursor> ChainCursors => Set<ChainCursor>();

	public DbSet<TokenDeployment> TokenDeployments => Set<TokenDeployment>();

	public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
}
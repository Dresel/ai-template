using FocusTemplate.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
}
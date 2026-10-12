using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Weather.Data;

public sealed class WeatherDbContext(DbContextOptions<WeatherDbContext> options) : WeatherDbContextBase(options);
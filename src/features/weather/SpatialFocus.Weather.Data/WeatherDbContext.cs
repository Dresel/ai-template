using Microsoft.EntityFrameworkCore;

namespace SpatialFocus.Weather.Data;

public sealed class WeatherDbContext(DbContextOptions<WeatherDbContext> options) : WeatherDbContextBase(options);
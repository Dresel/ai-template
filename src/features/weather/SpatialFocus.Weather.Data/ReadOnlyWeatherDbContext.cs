using Microsoft.EntityFrameworkCore;
using SpatialFocus.Data.Foundation;

namespace SpatialFocus.Weather.Data;

public sealed class ReadOnlyWeatherDbContext(DbContextOptions<ReadOnlyWeatherDbContext> options)
	: WeatherDbContextBase(options), IReadOnlyDbContext;
using FocusTemplate.Data.Foundation;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Weather.Data;

public sealed class ReadOnlyWeatherDbContext(DbContextOptions<ReadOnlyWeatherDbContext> options)
	: WeatherDbContextBase(options), IReadOnlyDbContext;
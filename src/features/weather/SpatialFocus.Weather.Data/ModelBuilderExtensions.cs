using Microsoft.EntityFrameworkCore;
using SpatialFocus.Data.Foundation.Identity;
using SpatialFocus.Weather.Data.Entities;

namespace SpatialFocus.Weather.Data;

public static class ModelBuilderExtensions
{
	public static ModelBuilder ApplyWeatherConfigurations(this ModelBuilder modelBuilder) =>
		modelBuilder.ApplyConfiguration(new UserReferenceConfiguration())
			.ApplyConfiguration(new StationConfiguration())
			.ApplyConfiguration(new ObservationConfiguration())
			.ApplyConfiguration(new AlertConfiguration())
			.ApplyConfiguration(new WeatherForecastConfiguration());
}
using FocusTemplate.Data.Foundation.Identity;
using FocusTemplate.Weather.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Weather.Data;

public static class ModelBuilderExtensions
{
	public static ModelBuilder ApplyWeatherConfigurations(this ModelBuilder modelBuilder) =>
		modelBuilder.ApplyConfiguration(new UserReferenceConfiguration())
			.ApplyConfiguration(new StationConfiguration())
			.ApplyConfiguration(new ObservationConfiguration())
			.ApplyConfiguration(new AlertConfiguration())
			.ApplyConfiguration(new WeatherForecastConfiguration());
}
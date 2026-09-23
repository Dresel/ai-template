using FocusTemplate.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

public static class ModelBuilderExtensions
{
	// Not ApplyConfigurationsFromAssembly: that scan is RequiresUnreferencedCode and invisible to trimming and NativeAOT.
	public static ModelBuilder ApplyEntityConfigurations(this ModelBuilder modelBuilder) =>
		modelBuilder.ApplyConfiguration(new StationConfiguration())
			.ApplyConfiguration(new ObservationConfiguration())
			.ApplyConfiguration(new AlertConfiguration())
			.ApplyConfiguration(new WeatherForecastConfiguration());
}
using FocusTemplate.Data.Foundation;
using FocusTemplate.Data.Foundation.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FocusTemplate.Weather.Data;

// What the EF tooling and the weather-migrations resource boot, as AppDbContextFactory does for the spine
public sealed class WeatherDbContextFactory : IDesignTimeDbContextFactory<WeatherDbContext>
{
	public WeatherDbContext CreateDbContext(string[] args)
	{
		DbContextOptionsBuilder<WeatherDbContext> optionsBuilder =
			new DbContextOptionsBuilder<WeatherDbContext>().ConfigureWeatherDbContext(
				DesignTime.ConnectionString,
				new AuditingInterceptor(TimeProvider.System, new FixedCurrentUser(WellKnownUsers.System)));

		if (DesignTime.SeedTestData)
		{
			optionsBuilder.UseWeatherSeeding();
		}

		return new WeatherDbContext(optionsBuilder.Options);
	}
}
using FocusTemplate.Data.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FocusTemplate.Data;

// What the EF tooling and the migrations resource boot. There is no container here, so the auditing interceptor is handed the system user directly.
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	public AppDbContext CreateDbContext(string[] args)
	{
		string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__focusdb") ??
			"Host=localhost;Database=focusdb";

		DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
			.ConfigureAppDbContext(
				connectionString,
				new AuditingInterceptor(TimeProvider.System, new FixedCurrentUser(WellKnownUsers.System)));

		if (bool.TryParse(Environment.GetEnvironmentVariable("Database__SeedTestData"), out bool seedTestData) &&
			seedTestData)
		{
			optionsBuilder.UseWeatherSeeding();
		}

		return new AppDbContext(optionsBuilder.Options);
	}
}
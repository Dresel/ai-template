using FocusTemplate.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SpatialFocus.Data;
using SpatialFocus.Data.Foundation;
using SpatialFocus.Data.Foundation.Auditing;

namespace FocusTemplate.Admin.Api;

// What the EF tooling and the AppHost's migrations resource boot. EF looks for it in the startup project, which for both
// is the Admin API. There is no container here, so the auditing interceptor is handed the system user directly.
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	public AppDbContext CreateDbContext(string[] args)
	{
		DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>().ConfigureAppDbContext(
			DesignTime.ConnectionString,
			new AuditingInterceptor(TimeProvider.System, new FixedCurrentUser(WellKnownUsers.System)))
			.UseProductMigrations();

		if (DesignTime.SeedTestData)
		{
			optionsBuilder.UseDevelopmentSeeding();
		}

		return new AppDbContext(optionsBuilder.Options);
	}
}
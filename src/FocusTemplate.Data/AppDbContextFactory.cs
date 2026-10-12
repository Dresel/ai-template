using FocusTemplate.Data.Foundation;
using FocusTemplate.Data.Foundation.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FocusTemplate.Data;

// What the EF tooling and the migrations resource boot. There is no container here, so the auditing interceptor is handed the system user directly.
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	public AppDbContext CreateDbContext(string[] args)
	{
		DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>().ConfigureAppDbContext(
			DesignTime.ConnectionString,
			new AuditingInterceptor(TimeProvider.System, new FixedCurrentUser(WellKnownUsers.System)));

		if (DesignTime.SeedTestData)
		{
			optionsBuilder.UseDevelopmentSeeding();
		}

		return new AppDbContext(optionsBuilder.Options);
	}
}
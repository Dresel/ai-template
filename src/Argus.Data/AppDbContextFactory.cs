using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Argus.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	public AppDbContext CreateDbContext(string[] args)
	{
		string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__argusdb") ?? "Host=localhost;Database=argusdb";

		DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString);

		if (bool.TryParse(Environment.GetEnvironmentVariable("Database__SeedTestData"), out bool seedTestData) && seedTestData)
		{
			optionsBuilder.UseWeatherSeeding();
		}

		return new AppDbContext(optionsBuilder.Options);
	}
}
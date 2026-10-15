using FocusTemplate.Admin.Primitives;
using FocusTemplate.Data;
using FocusTemplate.Names;
using Microsoft.EntityFrameworkCore;
using SpatialFocus.Data;
using SpatialFocus.Primitives;
using SpatialFocus.Weather.Data;

namespace FocusTemplate.Admin.Api.IntegrationTests;

// The product's API on the product's migrations, the spine's context first and the weather feature's after it, as the
// AppHost orders them
public sealed class ApiFixture(PostgresFixture postgres, RedisFixture redis) : ApiFixture<Program>(
	postgres,
	redis,
	ConnectionNames.Database,
	ConnectionNames.ReadOnlyDatabase,
	ConnectionNames.Redis)
{
	public override IReadOnlyList<Permission> Permissions => AdminPermissions.All;

	protected override void ConfigureAppDbContextOptions(DbContextOptionsBuilder<AppDbContext> options) => options.UseProductMigrations();

	protected override async Task CreateSchemaAsync(CancellationToken cancellationToken)
	{
		await using (AppDbContext dbContext = CreateDbContext())
		{
			await dbContext.Database.MigrateAsync(cancellationToken);
		}

		await using WeatherDbContext weather = new(
			new DbContextOptionsBuilder<WeatherDbContext>().ConfigureWeatherDbContext(ConnectionString, CreateAuditing()).Options);
		await weather.Database.MigrateAsync(cancellationToken);
	}
}
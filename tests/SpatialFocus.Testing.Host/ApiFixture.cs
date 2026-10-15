using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SpatialFocus.Data;
using SpatialFocus.Data.Entities;
using SpatialFocus.Primitives;
using SpatialFocus.Testing.Api;
using SpatialFocus.Weather.Data;

namespace SpatialFocus.Testing.Host;

// The spine's migrations are a product's, so the schema comes from the models: the spine's, the permission rows a
// product's migration seeds, then each package's tables beside them, in the AppHost's order.
public sealed class ApiFixture(PostgresFixture postgres, RedisFixture redis) : ApiFixture<Program>(
	postgres,
	redis,
	ConnectionNames.Database,
	ConnectionNames.ReadOnlyDatabase,
	ConnectionNames.Redis)
{
	public override IReadOnlyList<Permission> Permissions => TestHostPermissions.All;

	protected override async Task CreateSchemaAsync(CancellationToken cancellationToken)
	{
		await using (AppDbContext dbContext = CreateDbContext())
		{
			await dbContext.Database.EnsureCreatedAsync(cancellationToken);

			dbContext.Permissions.AddRange(Permissions.Select(permission => new PermissionDefinition { Name = permission, }));
			await dbContext.SaveChangesAsync(cancellationToken);
		}

		// EnsureCreated leaves a database with tables alone, so each further context creates only its own
		await using WeatherDbContext weather = new(
			new DbContextOptionsBuilder<WeatherDbContext>().ConfigureWeatherDbContext(ConnectionString, CreateAuditing()).Options);
		await weather.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(cancellationToken);
	}
}
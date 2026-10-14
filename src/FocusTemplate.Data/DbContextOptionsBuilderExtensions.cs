using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FocusTemplate.Data;

public static class DbContextOptionsBuilderExtensions
{
	// For what migrates the spine's AppDbContext: the design-time factory and the integration tests. The API's pools go
	// without, since nothing there migrates and the seeded rows change no query.
	public static TBuilder UseProductMigrations<TBuilder>(this TBuilder options)
		where TBuilder : DbContextOptionsBuilder
	{
		options.UseNpgsql(npgsql => npgsql.MigrationsAssembly(typeof(DbContextOptionsBuilderExtensions).Assembly));
		options.ReplaceService<IModelCustomizer, PermissionSeed>();

		return options;
	}
}
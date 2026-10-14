using Aspire.Hosting.EntityFrameworkCore;
using Projects;
using SpatialFocus.Data;
using SpatialFocus.Weather.Data;

namespace FocusTemplate.Admin.Web.E2E;

// Only the AppHost's model. It names the migrations' contexts as strings, so the compiler no longer checks them.
public sealed class MigrationResourceTests
{
	[Fact]
	public async Task EachMigrationsResourceNamesAContextThatExists()
	{
		await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder.CreateAsync<FocusTemplate_AppHost>(
			[
				"Features:TlsOffloadingIngress=false",
				"Features:Analytics=false",
				"Features:Mobile=false",
				"Features:PersistentLocalKeycloak=false",
				"Features:PersistentDatabase=false",
			],
			TestContext.Current.CancellationToken);

		Assert.Equal(
			[typeof(AppDbContext).FullName, typeof(WeatherDbContext).FullName,],
			builder.Resources.OfType<EFMigrationResource>().Select(resource => resource.DbContextTypeName).Order(StringComparer.Ordinal));
	}
}
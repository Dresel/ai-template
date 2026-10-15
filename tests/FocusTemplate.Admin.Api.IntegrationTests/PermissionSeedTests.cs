using FocusTemplate.Admin.Primitives;
using Microsoft.EntityFrameworkCore;
using SpatialFocus.Data;
using SpatialFocus.Primitives;

namespace FocusTemplate.Admin.Api.IntegrationTests;

// The product's migrations seed its permissions, which user management's grants point at
public sealed class PermissionSeedTests(ApiFixture factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task ThePermissionsTableHoldsEveryPermissionOfTheRelease()
	{
		await using AppDbContext dbContext = Factory.CreateDbContext();

		List<Permission> seeded = await dbContext.Permissions.Select(permission => permission.Name)
			.ToListAsync(TestContext.Current.CancellationToken);

		Assert.Equal(AdminPermissions.All.Order(), seeded.Order());
	}
}
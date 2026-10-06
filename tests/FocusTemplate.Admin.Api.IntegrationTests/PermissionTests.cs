using System.Net;
using FocusTemplate.Admin.Client.UserManagement.Users;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.IntegrationTests;

// The generated endpoints name the permissions, the groups in the database grant them, the handler of the requirement
// connects the two. A 403 the contract does not declare throws in the generated client.
public sealed class PermissionTests(ApiFixture factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task AnEndpointNamingAPermissionIsForbiddenWithoutIt()
	{
		User caller = await Factory.AddUserAsync();
		User other = await Factory.AddUserAsync();

		HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() =>
			new UsersClient(Factory.CreateAuthenticatedClient(caller.Id)).GetAsync(
				other.Id,
				TestContext.Current.CancellationToken));

		Assert.Equal(HttpStatusCode.Forbidden, error.StatusCode);
	}

	[Fact]
	public async Task TheGrantOfAGroupLetsItsMembersThrough()
	{
		User caller = await Factory.AddUserAsync();
		User other = await Factory.AddUserAsync("Other");
		await Factory.GrantAsync(caller.Id, UserManagementPermissions.ViewUsers);

		UsersGetResult result = await new UsersClient(Factory.CreateAuthenticatedClient(caller.Id)).GetAsync(
			other.Id,
			TestContext.Current.CancellationToken);

		Assert.True(result is UserResponse { DisplayName: "Other", }, $"Expected the user, got {result}");
	}

	[Fact]
	public async Task ADeactivatedUserHoldsNoPermissionWhateverTheirToken()
	{
		User caller = await Factory.AddUserAsync(isActive: false);
		await Factory.GrantAsync(caller.Id, UserManagementPermissions.ViewUsers);

		HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() =>
			new UsersClient(Factory.CreateAuthenticatedClient(caller.Id)).GetAsync(
				caller.Id,
				TestContext.Current.CancellationToken));

		Assert.Equal(HttpStatusCode.Forbidden, error.StatusCode);
	}

	// Every grant of every group, once and by name, so the UI can hide what the user may not do
	[Fact]
	public async Task TheCurrentUserNeedsNoPermissionAndListsWhatTheirGroupsGrant()
	{
		User caller = await Factory.AddUserAsync();
		await Factory.GrantAsync(caller.Id, UserManagementPermissions.ViewUsers, UserManagementPermissions.ViewGroups);
		await Factory.GrantAsync(caller.Id, UserManagementPermissions.ViewUsers);

		CurrentUserResponse me = await new UsersClient(Factory.CreateAuthenticatedClient(caller.Id)).MeAsync(
			TestContext.Current.CancellationToken);

		Assert.Equal(caller.Id, me.Id);
		Assert.Equal([UserManagementPermissions.ViewGroups, UserManagementPermissions.ViewUsers,], me.Permissions);
	}

	[Fact]
	public async Task ThePermissionsTableHoldsEveryPermissionOfTheRelease()
	{
		await using AppDbContext dbContext = Factory.CreateDbContext();

		List<Permission> seeded = await dbContext.Permissions.Select(permission => permission.Name)
			.ToListAsync(TestContext.Current.CancellationToken);

		Assert.Equal(Permission.All.Order(), seeded.Order());
	}
}
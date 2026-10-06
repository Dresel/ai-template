using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Client.UserManagement.Groups;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Data;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;
using Xunit.Sdk;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class GroupsTests(ApiFixture factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task ACreatedGroupStartsEmpty()
	{
		User caller = await AdminAsync();

		GroupsCreateResult result = await Client(caller)
			.CreateAsync(new GroupRequest("Editors", "Edit the content"), TestContext.Current.CancellationToken);

		GroupResponse group = result switch
		{
			GroupResponse value => value,
			_ => throw new XunitException($"Expected the group, got {result}"),
		};
		Assert.Equal(("Editors", "Edit the content", false), (group.Name, group.Description, group.IsManaged));
		Assert.Empty(group.Members);
		Assert.Empty(group.Permissions);
	}

	[Fact]
	public async Task AManagedGroupCannotBeDeleted()
	{
		User caller = await AdminAsync();
		await Factory.AddAdministratorsAsync();

		GroupsDeleteResult result = await Client(caller)
			.DeleteAsync(WellKnownGroups.Administrators, TestContext.Current.CancellationToken);

		Assert.True(result is ConflictProblem, $"Expected ConflictProblem, got {result}");
	}

	[Fact]
	public async Task AMemberAddedTwiceIsAMemberOnce()
	{
		User caller = await AdminAsync();
		User member = await Factory.AddUserAsync("Member");
		Group group = await Factory.GrantAsync(caller.Id);

		await Client(caller).AddMemberAsync(group.Id, member.Id, TestContext.Current.CancellationToken);
		GroupsAddMemberResult again =
			await Client(caller).AddMemberAsync(group.Id, member.Id, TestContext.Current.CancellationToken);

		Assert.True(again is Success, $"Expected Success, got {again}");
		GroupsGetResult result = await Client(caller).GetAsync(group.Id, TestContext.Current.CancellationToken);
		Assert.True(result is GroupResponse { MemberCount: 2, }, $"Expected the caller and the member, got {result}");
	}

	[Fact]
	public async Task AnAdministratorLeavesWhileAnotherStays()
	{
		User caller = await AdminAsync();
		User other = await Factory.AddUserAsync();
		await Factory.AddAdministratorsAsync(caller.Id, other.Id);

		GroupsRemoveMemberResult result = await Client(caller)
			.RemoveMemberAsync(WellKnownGroups.Administrators, other.Id, TestContext.Current.CancellationToken);

		Assert.True(result is Success, $"Expected Success, got {result}");
	}

	[Fact]
	public async Task AnUnknownUserCannotBecomeAMember()
	{
		User caller = await AdminAsync();
		Group group = await Factory.GrantAsync(caller.Id);

		GroupsAddMemberResult result = await Client(caller)
			.AddMemberAsync(group.Id, UserId.From(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

		Assert.True(result is NotFoundProblem, $"Expected NotFoundProblem, got {result}");
	}

	[Fact]
	public async Task AnotherManagedGroupCanLoseItsLastActiveMember()
	{
		User caller = await AdminAsync();
		User member = await Factory.AddUserAsync();
		Group group = await Factory.AddManagedGroupAsync(member.Id);

		GroupsRemoveMemberResult result = await Client(caller)
			.RemoveMemberAsync(group.Id, member.Id, TestContext.Current.CancellationToken);

		Assert.True(result is Success, $"Expected Success, got {result}");
	}

	[Fact]
	public async Task DeletingAGroupTakesAwayWhatItGranted()
	{
		User caller = await AdminAsync();
		User member = await Factory.AddUserAsync();
		Group group = await Factory.GrantAsync(member.Id, UserManagementPermissions.ViewUsers);

		GroupsDeleteResult result = await Client(caller).DeleteAsync(group.Id, TestContext.Current.CancellationToken);

		Assert.True(result is Success, $"Expected Success, got {result}");
		Assert.Empty(await GrantsOfAsync(group.Id));
		await using AppDbContext dbContext = Factory.CreateDbContext();
		Assert.False(
			await dbContext.GroupMembers.AnyAsync(
				entry => entry.GroupId == group.Id,
				TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task GrantsAreReplacedAsAWhole()
	{
		User caller = await AdminAsync();
		User member = await Factory.AddUserAsync();
		Group group = await Factory.GrantAsync(member.Id, UserManagementPermissions.ViewUsers, UserManagementPermissions.ViewGroups);

		GroupsSetPermissionsResult result = await Client(caller)
			.SetPermissionsAsync(
				group.Id,
				[UserManagementPermissions.ViewGroups, UserManagementPermissions.ManageGroups,],
				TestContext.Current.CancellationToken);

		Assert.True(result is Success, $"Expected Success, got {result}");
		Assert.Equal([UserManagementPermissions.ManageGroups, UserManagementPermissions.ViewGroups,], await GrantsOfAsync(group.Id));
	}

	// A permission an older release had and this one no longer knows. The client sends names, the database knows them
	[Fact]
	public async Task OnlyPermissionsOfTheReleaseCanBeGranted()
	{
		User caller = await AdminAsync();
		Group group = await Factory.GrantAsync(caller.Id);

		GroupsSetPermissionsResult result = await Client(caller)
			.SetPermissionsAsync(
				group.Id,
				[Permission.From("Retired.Permission"),],
				TestContext.Current.CancellationToken);

		Assert.True(result is ConflictProblem, $"Expected ConflictProblem, got {result}");
	}

	[Fact]
	public async Task TheAdministratorsKeepEveryPermission()
	{
		User caller = await AdminAsync();
		await Factory.AddAdministratorsAsync(caller.Id);

		GroupsSetPermissionsResult result = await Client(caller)
			.SetPermissionsAsync(WellKnownGroups.Administrators, [], TestContext.Current.CancellationToken);

		Assert.True(result is ConflictProblem, $"Expected ConflictProblem, got {result}");
		Assert.Equal(Permission.All.Count, (await GrantsOfAsync(WellKnownGroups.Administrators)).Count);
	}

	[Fact]
	public async Task TheLastActiveAdministratorCannotLeave()
	{
		User caller = await AdminAsync();
		User deactivated = await Factory.AddUserAsync(isActive: false);
		await Factory.AddAdministratorsAsync(caller.Id, deactivated.Id);

		GroupsRemoveMemberResult result = await Client(caller)
			.RemoveMemberAsync(WellKnownGroups.Administrators, caller.Id, TestContext.Current.CancellationToken);

		Assert.True(result is ConflictProblem, $"Expected ConflictProblem, got {result}");
	}

	[Fact]
	public async Task TwoGroupsCannotShareAName()
	{
		User caller = await AdminAsync();
		await Client(caller).CreateAsync(new GroupRequest("Editors"), TestContext.Current.CancellationToken);

		GroupsCreateResult result = await Client(caller)
			.CreateAsync(new GroupRequest("Editors"), TestContext.Current.CancellationToken);

		Assert.True(result is ConflictProblem, $"Expected ConflictProblem, got {result}");
	}

	// EF writes the row for the navigation, an instance of the audited join entity
	[Fact]
	public async Task WhoAddedAMemberIsAudited()
	{
		User caller = await AdminAsync();
		User member = await Factory.AddUserAsync("Member");
		Group group = await Factory.GrantAsync(caller.Id);

		await Client(caller).AddMemberAsync(group.Id, member.Id, TestContext.Current.CancellationToken);

		await using AppDbContext dbContext = Factory.CreateDbContext();
		UserId addedBy = await dbContext.GroupMembers
			.Where(entry => entry.GroupId == group.Id && entry.UserId == member.Id)
			.Select(entry => EF.Property<UserId>(entry, AuditingInterceptor.CreatedBy))
			.SingleAsync(TestContext.Current.CancellationToken);
		Assert.Equal(caller.Id, addedBy);
	}

	private async Task<User> AdminAsync()
	{
		User caller = await Factory.AddUserAsync("Admin");
		await Factory.GrantAsync(caller.Id, UserManagementPermissions.ViewGroups, UserManagementPermissions.ManageGroups);

		return caller;
	}

	private GroupsClient Client(User caller) => new(Factory.CreateAuthenticatedClient(caller.Id));

	private async Task<List<Permission>> GrantsOfAsync(GroupId group)
	{
		await using AppDbContext dbContext = Factory.CreateDbContext();

		return
		[
			.. (await dbContext.GroupPermissions.Where(grant => grant.GroupId == group)
				.Select(grant => grant.Permission)
				.ToListAsync(TestContext.Current.CancellationToken)).OrderBy(
				permission => permission.Value,
				StringComparer.Ordinal),
		];
	}
}
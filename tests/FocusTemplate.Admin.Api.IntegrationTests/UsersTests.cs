using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Client.UserManagement.Users;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Xunit.Sdk;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class UsersTests(ApiFixture factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task AUserDeactivatedAndReactivatedIsActiveAgain()
	{
		User caller = await AdminAsync();
		User other = await Factory.AddUserAsync();

		UsersDeactivateResult deactivated =
			await Client(caller).DeactivateAsync(other.Id, TestContext.Current.CancellationToken);
		bool activeAfterDeactivation = (await Factory.FindUserAsync(other.Id)).IsActive;
		UsersActivateResult activated =
			await Client(caller).ActivateAsync(other.Id, TestContext.Current.CancellationToken);

		Assert.True(deactivated is Success, $"Expected Success, got {deactivated}");
		Assert.False(activeAfterDeactivation);
		Assert.True(activated is Success, $"Expected Success, got {activated}");
		Assert.True((await Factory.FindUserAsync(other.Id)).IsActive);
	}

	[Fact]
	public async Task GetAnswersTheGroupsAndWhatTheyGrant()
	{
		User caller = await AdminAsync();
		User other = await Factory.AddUserAsync("Other");
		Group group = await Factory.GrantAsync(other.Id, UserManagementPermissions.ViewGroups);

		UsersGetResult result = await Client(caller).GetAsync(other.Id, TestContext.Current.CancellationToken);

		UserResponse user = result switch
		{
			UserResponse value => value,
			_ => throw new XunitException($"Expected the user, got {result}"),
		};
		Assert.Equal([new GroupReferenceResponse(group.Id, group.Name),], user.Groups);
		Assert.Equal([UserManagementPermissions.ViewGroups,], user.Permissions);
	}

	[Fact]
	public async Task GetOfAMissingUserIsNotFound()
	{
		User caller = await AdminAsync();

		UsersGetResult result = await Client(caller)
			.GetAsync(UserId.From(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

		Assert.True(result is NotFoundProblem, $"Expected NotFoundProblem, got {result}");
	}

	[Fact]
	public async Task NobodyDeactivatesThemselves()
	{
		User caller = await AdminAsync();

		UsersDeactivateResult result =
			await Client(caller).DeactivateAsync(caller.Id, TestContext.Current.CancellationToken);

		Assert.True(result is ConflictProblem, $"Expected ConflictProblem, got {result}");
	}

	[Fact]
	public async Task SearchFindsPartOfTheNameOrTheEMailAddressAndCountsAllMatches()
	{
		User caller = await AdminAsync();
		await Factory.AddUserAsync("Anna Berger");
		await Factory.AddUserAsync("Bernd Huber", email: "bernd@example.com");
		await Factory.AddUserAsync("Clara Ott", email: "clara@berg.example");

		UserPageResponse page = await SearchAsync(caller, new UserSearchRequest("berg", Top: 1));

		Assert.Equal(2, page.Total);
		Assert.Equal(["Anna Berger",], page.Items.Select(user => user.DisplayName));
	}

	[Fact]
	public async Task SearchOrdersByTheEMailAddressWithUsersWithoutOneLast()
	{
		User caller = await AdminAsync();
		await Factory.AddUserAsync("Bea", email: "bea@example.com");
		await Factory.AddUserAsync("Ada", email: "ada@example.com");
		await Factory.AddUserAsync("Nobody");

		UserPageResponse ascending = await SearchAsync(caller, new UserSearchRequest(Sort: UserSort.Email));
		UserPageResponse descending = await SearchAsync(
			caller,
			new UserSearchRequest(Sort: UserSort.Email, Descending: true));

		Assert.Equal(["ada@example.com", "bea@example.com", null, null,], ascending.Items.Select(user => user.Email));
		Assert.Equal(["bea@example.com", "ada@example.com", null, null,], descending.Items.Select(user => user.Email));
	}

	[Fact]
	public async Task SearchOrdersByTheLastVisitWhenAskedAndPages()
	{
		DateTimeOffset now = Factory.Clock.GetUtcNow();
		User caller = await AdminAsync();
		await Factory.AddUserAsync("Early", lastSeenAt: now.AddDays(-2));
		await Factory.AddUserAsync("Late", lastSeenAt: now.AddDays(-1));

		UserPageResponse page = await SearchAsync(
			caller,
			new UserSearchRequest(Sort: UserSort.LastSeenAt, Descending: true, Skip: 1, Top: 2));

		Assert.Equal(3, page.Total);
		Assert.Equal(["Late", "Early",], page.Items.Select(user => user.DisplayName));
	}

	// The percent sign is a wildcard in LIKE, the handler searches for it literally
	[Fact]
	public async Task SearchTakesWildcardsLiterally()
	{
		User caller = await AdminAsync();
		await Factory.AddUserAsync("50% Club");
		await Factory.AddUserAsync("500 Club");

		UserPageResponse page = await SearchAsync(caller, new UserSearchRequest("50%"));

		Assert.Equal(["50% Club",], page.Items.Select(user => user.DisplayName));
	}

	[Fact]
	public async Task TheLastActiveAdministratorCannotBeDeactivated()
	{
		User caller = await Factory.AddUserAsync();
		await Factory.GrantAsync(caller.Id, UserManagementPermissions.ManageUsers);
		User administrator = await Factory.AddUserAsync();
		User deactivated = await Factory.AddUserAsync(isActive: false);
		await Factory.AddAdministratorsAsync(administrator.Id, deactivated.Id);

		UsersDeactivateResult result =
			await Client(caller).DeactivateAsync(administrator.Id, TestContext.Current.CancellationToken);

		Assert.True(result is ConflictProblem, $"Expected ConflictProblem, got {result}");
		Assert.True((await Factory.FindUserAsync(administrator.Id)).IsActive);
	}

	[Fact]
	public async Task TheLastActiveMemberOfAnotherManagedGroupCanBeDeactivated()
	{
		User caller = await Factory.AddUserAsync();
		await Factory.GrantAsync(caller.Id, UserManagementPermissions.ManageUsers);
		User member = await Factory.AddUserAsync();
		await Factory.AddManagedGroupAsync(member.Id);

		UsersDeactivateResult result =
			await Client(caller).DeactivateAsync(member.Id, TestContext.Current.CancellationToken);

		Assert.True(result is Success, $"Expected Success, got {result}");
		Assert.False((await Factory.FindUserAsync(member.Id)).IsActive);
	}

	private async Task<User> AdminAsync()
	{
		User caller = await Factory.AddUserAsync("Admin");
		await Factory.GrantAsync(caller.Id, UserManagementPermissions.ViewUsers, UserManagementPermissions.ManageUsers);

		return caller;
	}

	private UsersClient Client(User caller) => new(Factory.CreateAuthenticatedClient(caller.Id));

	private async Task<UserPageResponse> SearchAsync(User caller, UserSearchRequest request)
	{
		UsersSearchResult result = await Client(caller).SearchAsync(request, TestContext.Current.CancellationToken);

		return result switch
		{
			UserPageResponse page => page,
			_ => throw new XunitException($"Expected a page, got {result}"),
		};
	}
}
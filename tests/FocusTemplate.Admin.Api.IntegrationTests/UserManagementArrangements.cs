using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.IntegrationTests;

// A fresh id per user, since the provisioning middleware remembers whom it has seen for its window across the tests of a
// class, and the reset in between removes the rows it would otherwise find.
public static class UserManagementArrangements
{
	public static async Task<User> AddUserAsync(
		this ApiFixture factory,
		string displayName = "Someone",
		bool isActive = true,
		DateTimeOffset? lastSeenAt = null,
		string? email = null)
	{
		User user = new()
		{
			Id = UserId.From(Guid.CreateVersion7()),
			DisplayName = displayName,
			Email = email,
			IsActive = isActive,
			FirstSeenAt = factory.Clock.GetUtcNow(),
		};

		await using AppDbContext dbContext = factory.CreateDbContext();
		dbContext.Users.Add(user);
		dbContext.UserActivities.Add(
			new UserActivity { UserId = user.Id, LastSeenAt = lastSeenAt ?? factory.Clock.GetUtcNow(), });
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

		return user;
	}

	// A group of its own granting the permissions, with the user as its member
	public static async Task<Group> GrantAsync(this ApiFixture factory, UserId user, params Permission[] permissions)
	{
		Group group = new() { Id = GroupId.New(), Name = $"Grants {Guid.NewGuid():N}", };

		await using AppDbContext dbContext = factory.CreateDbContext();
		dbContext.Groups.Add(group);
		dbContext.GroupPermissions.AddRange(
			permissions.Select(permission => new GroupPermission { GroupId = group.Id, Permission = permission, }));
		dbContext.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = user, });
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

		return group;
	}

	// What the migrations seed and the reset removes: the managed group, holding every permission
	public static async Task AddAdministratorsAsync(this ApiFixture factory, params UserId[] members)
	{
		await using AppDbContext dbContext = factory.CreateDbContext();
		dbContext.Groups.Add(
			new Group { Id = WellKnownGroups.Administrators, Name = "Administrators", IsManaged = true, });
		dbContext.GroupPermissions.AddRange(
			Permission.All.Select(permission => new GroupPermission
			{
				GroupId = WellKnownGroups.Administrators, Permission = permission,
			}));
		dbContext.GroupMembers.AddRange(
			members.Select(member => new GroupMember { GroupId = WellKnownGroups.Administrators, UserId = member, }));
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
	}

	public static async Task<User> FindUserAsync(this ApiFixture factory, UserId id)
	{
		await using AppDbContext dbContext = factory.CreateDbContext();

		return await dbContext.Users.SingleAsync(user => user.Id == id, TestContext.Current.CancellationToken);
	}
}
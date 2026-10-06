using Bogus;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

// Made-up users to page and search through, and two groups granting part of the user management. The fixed seed makes
// them the same on every start, which the check for the first of them relies on. None of them can sign in.
internal static class UserManagementSeed
{
	private const int Count = 48;

	internal static bool Seed(DbContext context)
	{
		Faker faker = new() { Random = new Randomizer(20261002), };
		DateTimeOffset now = DateTimeOffset.UtcNow;

		List<User> users = [.. Enumerable.Range(0, Count).Select(_ => MakeUp(faker, now)),];
		if (context.Set<User>().Any(user => user.Id == users[0].Id))
		{
			return false;
		}

		context.Set<User>().AddRange(users);
		AddGroup(context, faker, "Support", "Look up users and their groups.", users[..8], UserManagementPermissions.ViewUsers, UserManagementPermissions.ViewGroups);
		AddGroup(context, faker, "User administration", "Deactivate and reactivate users.", users[8..11], UserManagementPermissions.ViewUsers, UserManagementPermissions.ManageUsers);

		return true;
	}

	// The name is unique, and in a kept database someone may have taken it. Their group stays, the seed's is left out
	private static void AddGroup(
		DbContext context,
		Faker faker,
		string name,
		string description,
		IEnumerable<User> members,
		params Permission[] permissions)
	{
		GroupId id = GroupId.From(faker.Random.Guid());
		if (context.Set<Group>().Any(group => group.Name == name))
		{
			return;
		}

		context.Set<Group>().Add(new Group { Id = id, Name = name, Description = description, });
		context.Set<GroupPermission>()
			.AddRange(permissions.Select(permission => new GroupPermission { GroupId = id, Permission = permission, }));
		context.Set<GroupMember>().AddRange(members.Select(member => new GroupMember { GroupId = id, UserId = member.Id, }));
	}

	private static User MakeUp(Faker faker, DateTimeOffset now)
	{
		UserId id = UserId.From(faker.Random.Guid());
		string firstName = faker.Name.FirstName();
		string lastName = faker.Name.LastName();
		DateTimeOffset firstSeenAt = now - TimeSpan.FromDays(faker.Random.Double(30, 365));

		return new User
		{
			Id = id,
			DisplayName = $"{firstName} {lastName}",
			Email = faker.Internet.Email(firstName, lastName, "example.com").ToLowerInvariant(),
			FirstSeenAt = firstSeenAt,
			IsActive = faker.Random.Bool(0.9f),
			Activity = new UserActivity { UserId = id, LastSeenAt = faker.Date.BetweenOffset(firstSeenAt, now), },
		};
	}
}
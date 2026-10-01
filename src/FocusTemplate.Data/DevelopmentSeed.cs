using FocusTemplate.Data.Auditing;
using FocusTemplate.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

public static class DevelopmentSeed
{
	// See https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding#configuration-options-useseeding-and-useasyncseeding-methods
	public static DbContextOptionsBuilder UseDevelopmentSeeding(this DbContextOptionsBuilder optionsBuilder) =>
		optionsBuilder.UseSeeding((context, _) =>
			{
				if (Seed(context))
				{
					context.SaveChanges();
				}
			})
			.UseAsyncSeeding(async (context, _, cancellationToken) =>
			{
				if (Seed(context))
				{
					await context.SaveChangesAsync(cancellationToken);
				}
			});

	// Shared by both seed delegates - the EF CLI calls the synchronous one, so neither may be left out. Not
	// short-circuited: each part checks its own rows, so a persistent database still gets a part added later.
	private static bool Seed(DbContext context) => WeatherSeed.Seed(context) | SeedDeveloperAsAdministrator(context);

	// The developer login of the local realm administers a fresh database, so the user management needs no bootstrap.
	private static bool SeedDeveloperAsAdministrator(DbContext context)
	{
		if (context.Set<GroupMember>()
			.Any(member => member.GroupId == WellKnownGroups.Administrators && member.UserId == WellKnownUsers.Developer))
		{
			return false;
		}

		DateTimeOffset now = DateTimeOffset.UtcNow;
		if (!context.Set<User>().Any(user => user.Id == WellKnownUsers.Developer))
		{
			context.Set<User>()
				.Add(new User { Id = WellKnownUsers.Developer, DisplayName = "developer", FirstSeenAt = now, });
			context.Set<UserActivity>().Add(new UserActivity { UserId = WellKnownUsers.Developer, LastSeenAt = now, });
		}

		context.Set<GroupMember>()
			.Add(new GroupMember { GroupId = WellKnownGroups.Administrators, UserId = WellKnownUsers.Developer, });

		return true;
	}
}
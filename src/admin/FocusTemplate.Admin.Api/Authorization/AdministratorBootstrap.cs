using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Authorization;

// What the one-off bootstrap job runs: the subject becomes an active administrator, before or after their first sign-in,
// the local user id being the subject. Idempotent, and audited as System, since no request is in flight.
public static class AdministratorBootstrap
{
	public const string Command = "bootstrap-admin";

	public static async Task PromoteAsync(IServiceProvider services, UserId subject, CancellationToken cancellationToken)
	{
		await using AsyncServiceScope scope = services.CreateAsyncScope();
		AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		DateTimeOffset now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();

		User? user = await dbContext.Users
			.Include(entity => entity.Groups.Where(group => group.Id == WellKnownGroups.Administrators))
			.SingleOrDefaultAsync(entity => entity.Id == subject, cancellationToken);

		if (user is null)
		{
			// The first sign-in replaces the placeholder name with the one from the token
			user = new User
			{
				Id = subject,
				DisplayName = subject.ToString(),
				FirstSeenAt = now,
				Activity = new UserActivity { UserId = subject, LastSeenAt = now, },
			};
			dbContext.Users.Add(user);
		}
		else
		{
			user.IsActive = true;
		}

		if (user.Groups.Count == 0)
		{
			user.Groups.Add(
				await dbContext.Groups.SingleAsync(group => group.Id == WellKnownGroups.Administrators, cancellationToken));
		}

		await dbContext.SaveChangesAsync(cancellationToken);
	}
}
using System.Security.Claims;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FocusTemplate.Admin.Api.Authentication;

public sealed class UserProvisioningMiddleware(RequestDelegate next, TimeProvider clock, IMemoryCache cache)
{
	private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

	public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
	{
		DateTimeOffset now = clock.GetUtcNow();

		// The cache expires entries on the system clock, which only evicts them. The window is measured on TimeProvider
		if (context.User.UserIdOrDefault is { } id &&
			(!cache.TryGetValue(new Seen(id), out DateTimeOffset last) || now - last >= Window))
		{
			await ProvisionAsync(dbContext, context.User, id, now, context.RequestAborted);
			cache.Set(new Seen(id), now, Window);
		}

		await next(context);
	}

	private static async Task ProvisionAsync(
		AppDbContext dbContext,
		ClaimsPrincipal principal,
		UserId id,
		DateTimeOffset now,
		CancellationToken cancellationToken)
	{
		string displayName = Truncate(
			principal.FindFirstValue(JwtRegisteredClaimNames.Name) ?? principal.Identity?.Name ?? id.ToString(),
			User.DisplayNameLength);
		string? email = principal.FindFirstValue(JwtRegisteredClaimNames.Email);

		User? user = await dbContext.Users.Include(entity => entity.Activity)
			.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
		if (user is null)
		{
			dbContext.Users.Add(
				new User
				{
					Id = id,
					DisplayName = displayName,
					Email = email,
					FirstSeenAt = now,
					Activity = new UserActivity { UserId = id, LastSeenAt = now, },
				});
		}
		else
		{
			// Unchanged values mark nothing modified, so only a rename in the identity provider reaches the audit columns
			user.DisplayName = displayName;
			user.Email = email;

			user.Activity ??= new UserActivity { UserId = id, LastSeenAt = now, };
			user.Activity.LastSeenAt = now;
		}

		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException) when (user is null)
		{
			// A parallel first request inserted the user. The request goes on with the pooled context, so nothing may stay tracked
			dbContext.ChangeTracker.Clear();
		}
	}

	private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];

	// A key of its own type, so no other entry of the shared cache can collide with a user id
	private readonly record struct Seen(UserId User);
}
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FocusTemplate.Data.Auditing;

// An interceptor rather than a database trigger, because only the application knows who is acting. The price is that
// ExecuteUpdate and raw SQL bypass it - see the auditing convention in AGENTS.md.
public sealed class AuditingInterceptor(TimeProvider timeProvider, ICurrentUser currentUser) : SaveChangesInterceptor
{
	public const string CreatedAt = nameof(CreatedAt);

	public const string CreatedBy = nameof(CreatedBy);

	public const string UpdatedAt = nameof(UpdatedAt);

	public const string UpdatedBy = nameof(UpdatedBy);

	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
	{
		SetAuditColumns(eventData.Context);

		return base.SavingChanges(eventData, result);
	}

	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
		DbContextEventData eventData,
		InterceptionResult<int> result,
		CancellationToken cancellationToken = default)
	{
		SetAuditColumns(eventData.Context);

		return base.SavingChangesAsync(eventData, result, cancellationToken);
	}

	private void SetAuditColumns(DbContext? context)
	{
		if (context is null)
		{
			return;
		}

		DateTimeOffset now = timeProvider.GetUtcNow();
		UserId user = currentUser.Id;

		foreach (EntityEntry<IAuditable> entry in context.ChangeTracker.Entries<IAuditable>())
		{
			if (entry.State == EntityState.Added)
			{
				entry.Property(CreatedAt).CurrentValue = now;
				entry.Property(CreatedBy).CurrentValue = user;
				entry.Property(UpdatedAt).CurrentValue = now;
				entry.Property(UpdatedBy).CurrentValue = user;
			}
			else if (entry.State == EntityState.Modified)
			{
				entry.Property(UpdatedAt).CurrentValue = now;
				entry.Property(UpdatedBy).CurrentValue = user;
			}
		}
	}
}
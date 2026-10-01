using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

// The SaveChanges guard fails early, but only a read-only database user role stops ExecuteUpdate, ExecuteDelete and raw SQL.
public sealed class ReadOnlyAppDbContext(DbContextOptions<ReadOnlyAppDbContext> options) : AppDbContextBase(options)
{
	public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
		throw new InvalidOperationException("This context is read-only.");

	public override Task<int> SaveChangesAsync(
		bool acceptAllChangesOnSuccess,
		CancellationToken cancellationToken = default) =>
		throw new InvalidOperationException("This context is read-only.");
}
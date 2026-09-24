using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

// Read-only db context for queries only, guarded by read-only database user.
public sealed class ReadOnlyAppDbContext(DbContextOptions<ReadOnlyAppDbContext> options) : AppDbContextBase(options)
{
	public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
		throw new InvalidOperationException("This context is read-only.");

	public override Task<int> SaveChangesAsync(
		bool acceptAllChangesOnSuccess,
		CancellationToken cancellationToken = default) =>
		throw new InvalidOperationException("This context is read-only.");
}
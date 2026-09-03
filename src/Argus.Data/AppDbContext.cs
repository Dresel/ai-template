using Argus.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Argus.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<ChainCursor> ChainCursors => Set<ChainCursor>();

	public DbSet<TokenDeployment> TokenDeployments => Set<TokenDeployment>();
}
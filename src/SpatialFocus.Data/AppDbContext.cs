using Microsoft.EntityFrameworkCore;

namespace SpatialFocus.Data;

// Writable db context for command and queries.
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : AppDbContextBase(options);
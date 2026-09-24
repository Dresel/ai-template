using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

// Writable db context for command and queries.
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : AppDbContextBase(options);
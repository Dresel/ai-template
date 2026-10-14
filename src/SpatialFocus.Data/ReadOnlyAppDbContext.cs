using Microsoft.EntityFrameworkCore;
using SpatialFocus.Data.Foundation;

namespace SpatialFocus.Data;

public sealed class ReadOnlyAppDbContext(DbContextOptions<ReadOnlyAppDbContext> options) : AppDbContextBase(options), IReadOnlyDbContext;
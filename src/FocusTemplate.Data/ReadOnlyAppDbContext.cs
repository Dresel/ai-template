using FocusTemplate.Data.Foundation;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

public sealed class ReadOnlyAppDbContext(DbContextOptions<ReadOnlyAppDbContext> options) : AppDbContextBase(options), IReadOnlyDbContext;
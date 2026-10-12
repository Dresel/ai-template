namespace FocusTemplate.Data.Foundation;

// Marks a context that tracks nothing and whose SaveChanges FoundationDbContext refuses. Query handlers take one,
// command handlers never do, which HandlerDbContextTests checks for every API and feature.
public interface IReadOnlyDbContext;
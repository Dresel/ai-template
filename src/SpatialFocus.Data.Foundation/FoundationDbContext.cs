using Microsoft.EntityFrameworkCore;
using SpatialFocus.Data.Foundation.Auditing;

namespace SpatialFocus.Data.Foundation;

// Every context's model: the foundation's conventions first, the default schema, and the audit columns last, so they
// reach every entity. TModel is the model's base class, which the writable and read-only contexts share.
public abstract class FoundationDbContext<TModel>(DbContextOptions options) : DbContext(options)
	where TModel : FoundationDbContext<TModel>, IHasSchema
{
	// Postgres's own, where the spine's tables live, so the spine's model leaves it unnamed
	public const string DefaultSchema = "public";

	// Fails a read-only context before any SQL. Only its database role stops ExecuteUpdate, ExecuteDelete and raw SQL.
	public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
		this is IReadOnlyDbContext
			? throw new InvalidOperationException("This context is read-only.")
			: base.SaveChanges(acceptAllChangesOnSuccess);

	public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
		this is IReadOnlyDbContext
			? throw new InvalidOperationException("This context is read-only.")
			: base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

	protected sealed override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder.AddFoundationConventions();

		ConfigureIds(configurationBuilder);
	}

	// The converters and sentinels of the context's own typed ids
	protected abstract void ConfigureIds(ModelConfigurationBuilder configurationBuilder);

	// The entity configurations and whatever else the model declares, such as a Postgres extension
	protected abstract void ConfigureModel(ModelBuilder modelBuilder);

	protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
	{
		if (TModel.Schema != DefaultSchema)
		{
			modelBuilder.HasDefaultSchema(TModel.Schema);
		}

		ConfigureModel(modelBuilder);
		modelBuilder.AddAuditingShadowProperties();
	}
}
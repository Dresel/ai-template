using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FocusTemplate.Data.Auditing;

public static class ModelBuilderExtensions
{
	// Shadow properties rather than members: the four columns are in the model and the migrations, but an entity cannot read or set them directly.
	public static ModelBuilder AddAuditingShadowProperties(this ModelBuilder modelBuilder)
	{
		foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
		{
			if (!typeof(IAuditable).IsAssignableFrom(entityType.ClrType))
			{
				continue;
			}

			modelBuilder.Entity(entityType.ClrType).Property<DateTimeOffset>(AuditingInterceptor.CreatedAt);
			modelBuilder.Entity(entityType.ClrType).Property<UserId>(AuditingInterceptor.CreatedBy);
			modelBuilder.Entity(entityType.ClrType).Property<DateTimeOffset>(AuditingInterceptor.UpdatedAt);
			modelBuilder.Entity(entityType.ClrType).Property<UserId>(AuditingInterceptor.UpdatedBy);
		}

		return modelBuilder;
	}
}
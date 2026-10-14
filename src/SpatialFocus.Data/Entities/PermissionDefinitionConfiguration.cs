using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SpatialFocus.Data.Entities;

// The rows are the product's permissions, which the product that owns the migrations seeds
public sealed class PermissionDefinitionConfiguration : IEntityTypeConfiguration<PermissionDefinition>
{
	public void Configure(EntityTypeBuilder<PermissionDefinition> builder) => builder.HasKey(p => p.Name);
}
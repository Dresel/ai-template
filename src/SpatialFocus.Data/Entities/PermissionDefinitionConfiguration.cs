using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpatialFocus.Primitives;

namespace SpatialFocus.Data.Entities;

public sealed class PermissionDefinitionConfiguration : IEntityTypeConfiguration<PermissionDefinition>
{
	public void Configure(EntityTypeBuilder<PermissionDefinition> builder)
	{
		builder.HasKey(p => p.Name);

		builder.HasData(Permission.All.Select(name => new PermissionDefinition { Name = name, }));
	}
}
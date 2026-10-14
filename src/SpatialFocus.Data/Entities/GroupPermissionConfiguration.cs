using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SpatialFocus.Data.Entities;

public sealed class GroupPermissionConfiguration : IEntityTypeConfiguration<GroupPermission>
{
	public void Configure(EntityTypeBuilder<GroupPermission> builder) => builder.HasKey(g => new { g.GroupId, g.Permission, });
}
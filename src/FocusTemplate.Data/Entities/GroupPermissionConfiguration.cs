using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusTemplate.Data.Entities;

public sealed class GroupPermissionConfiguration : IEntityTypeConfiguration<GroupPermission>
{
	public void Configure(EntityTypeBuilder<GroupPermission> builder)
	{
		builder.HasKey(g => new { g.GroupId, g.Permission, });

		// Seed the well-known group permissions for the Administrators group
		builder.HasData(
			Permission.All.Select(permission => new
			{
				GroupId = WellKnownGroups.Administrators,
				Permission = permission,
				CreatedAt = HasDataAudit.Timestamp,
				CreatedBy = WellKnownUsers.System,
				UpdatedAt = HasDataAudit.Timestamp,
				UpdatedBy = WellKnownUsers.System,
			}));
	}
}
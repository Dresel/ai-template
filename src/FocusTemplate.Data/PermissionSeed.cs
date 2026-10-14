using FocusTemplate.Admin.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SpatialFocus.Data.Auditing;
using SpatialFocus.Data.Entities;
using SpatialFocus.Data.Foundation.Auditing;

namespace FocusTemplate.Data;

// The product's permissions as rows of the spine's tables, each granted to the administrators. They go into the
// migrations this product owns, so a permission the product gains or loses is a migration here.
internal sealed class PermissionSeed(ModelCustomizerDependencies dependencies) : RelationalModelCustomizer(dependencies)
{
	public override void Customize(ModelBuilder modelBuilder, DbContext context)
	{
		base.Customize(modelBuilder, context);

		modelBuilder.Entity<PermissionDefinition>()
			.HasData(AdminPermissions.All.Select(name => new PermissionDefinition { Name = name, }));

		modelBuilder.Entity<GroupPermission>()
			.HasData(
				AdminPermissions.All.Select(permission => new
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
using FocusTemplate.Data.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusTemplate.Data.Entities;

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
	public void Configure(EntityTypeBuilder<Group> builder)
	{
		builder.Property(g => g.Id).ValueGeneratedNever();

		builder.HasIndex(g => g.Name).IsUnique();

		builder.HasMany(g => g.Members)
			.WithMany(u => u.Groups)
			.UsingEntity<GroupMember>(
				l => l.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict),
				r => r.HasOne<Group>().WithMany().HasForeignKey(m => m.GroupId).OnDelete(DeleteBehavior.Cascade));

		builder.HasMany(g => g.Permissions)
			.WithMany()
			.UsingEntity<GroupPermission>(
				l => l.HasOne<PermissionDefinition>()
					.WithMany()
					.HasForeignKey(p => p.Permission)
					.OnDelete(DeleteBehavior.Cascade),
				r => r.HasOne<Group>().WithMany().HasForeignKey(p => p.GroupId).OnDelete(DeleteBehavior.Cascade));

		builder.HasData(
			new
			{
				Id = WellKnownGroups.Administrators,
				Name = "Administrators",
				Description = "Everything the application can do. Managed: its permissions come with each release.",
				IsManaged = true,
				CreatedAt = HasDataAudit.Timestamp,
				CreatedBy = WellKnownUsers.System,
				UpdatedAt = HasDataAudit.Timestamp,
				UpdatedBy = WellKnownUsers.System,
			});
	}
}
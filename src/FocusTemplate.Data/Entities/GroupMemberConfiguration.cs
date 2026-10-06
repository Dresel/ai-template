using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusTemplate.Data.Entities;

public sealed class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
	public void Configure(EntityTypeBuilder<GroupMember> builder)
	{
		builder.HasKey(m => new { m.GroupId, m.UserId, });

		builder.HasIndex(m => m.UserId);
	}
}
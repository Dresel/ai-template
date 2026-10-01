using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusTemplate.Data.Entities;

public sealed class UserActivityConfiguration : IEntityTypeConfiguration<UserActivity>
{
	public void Configure(EntityTypeBuilder<UserActivity> builder)
	{
		builder.HasKey(a => a.UserId);
		builder.Property(a => a.UserId).ValueGeneratedNever();

		builder.HasOne<User>()
			.WithOne(u => u.Activity)
			.HasForeignKey<UserActivity>(a => a.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
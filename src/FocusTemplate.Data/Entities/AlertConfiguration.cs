using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusTemplate.Data.Entities;

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
	public void Configure(EntityTypeBuilder<Alert> builder)
	{
		builder.Property(a => a.Id).ValueGeneratedNever();

		builder.HasOne<Station>().WithMany().HasForeignKey(a => a.StationId).OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(a => new { a.StationId, a.ResolvedAt, });
	}
}
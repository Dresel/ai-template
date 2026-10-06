using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusTemplate.Data.Entities;

public sealed class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
	public void Configure(EntityTypeBuilder<Observation> builder)
	{
		builder.Property(o => o.Id).ValueGeneratedOnAdd();

		builder.HasOne<Station>().WithMany().HasForeignKey(o => o.StationId).OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(o => new { o.StationId, o.MeasuredAt, }).IsDescending(false, true);
	}
}
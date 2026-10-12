using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusTemplate.Weather.Data.Entities;

public sealed class StationConfiguration : IEntityTypeConfiguration<Station>
{
	public void Configure(EntityTypeBuilder<Station> builder)
	{
		builder.Property(s => s.Id).ValueGeneratedNever();

		builder.Property(s => s.Location).HasColumnType("geography (point)");

		// A deactivated user keeps their row, and a station outlives its owner's activity
		builder.HasOne(s => s.Owner).WithMany().HasForeignKey(s => s.OwnerId).OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(s => new { s.OwnerId, s.Code, }).IsUnique();
	}
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusTemplate.Data.Entities;

public sealed class StationConfiguration : IEntityTypeConfiguration<Station>
{
	public void Configure(EntityTypeBuilder<Station> builder)
	{
		builder.Property(s => s.Id).ValueGeneratedNever();

		builder.Property(s => s.Location).HasColumnType("geography (point)");

		builder.HasIndex(s => new { s.OwnerId, s.Code, }).IsUnique();
	}
}
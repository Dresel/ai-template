using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FocusTemplate.Data.Entities;

public sealed class WeatherForecastConfiguration : IEntityTypeConfiguration<WeatherForecast>
{
	public void Configure(EntityTypeBuilder<WeatherForecast> builder)
	{
		builder.Property(f => f.Id).ValueGeneratedOnAdd().HasSentinel(WeatherForecastId.Unspecified);

		builder.HasOne<Station>().WithMany().HasForeignKey(f => f.StationId).OnDelete(DeleteBehavior.Cascade);
		builder.HasIndex(f => new { f.StationId, f.Date, });
	}
}
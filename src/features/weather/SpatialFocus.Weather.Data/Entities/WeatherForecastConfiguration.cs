using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpatialFocus.Weather.Primitives;

namespace SpatialFocus.Weather.Data.Entities;

public sealed class WeatherForecastConfiguration : IEntityTypeConfiguration<WeatherForecast>
{
	public void Configure(EntityTypeBuilder<WeatherForecast> builder)
	{
		builder.Property(f => f.Id).ValueGeneratedOnAdd();

		builder.HasOne<Station>().WithMany().HasForeignKey(f => f.StationId).OnDelete(DeleteBehavior.Cascade);
		builder.HasIndex(f => new { f.StationId, f.Date, });
	}
}
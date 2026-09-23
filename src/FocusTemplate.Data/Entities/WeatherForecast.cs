using System.ComponentModel.DataAnnotations;
using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Entities;

public sealed class WeatherForecast
{
	public WeatherForecastId Id { get; set; } = WeatherForecastId.Unspecified;

	public required StationId StationId { get; init; }

	public DateOnly Date { get; set; }

	[MaxLength(200)]
	public string? Summary { get; set; }

	public int TemperatureC { get; set; }
}
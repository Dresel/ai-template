using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Entities;

public sealed class WeatherForecast
{
	public WeatherForecastId Id { get; set; } = WeatherForecastId.Unspecified;

	public DateOnly Date { get; set; }

	public string? Summary { get; set; }

	public int TemperatureC { get; set; }
}
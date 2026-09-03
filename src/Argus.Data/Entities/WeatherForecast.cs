namespace Argus.Data.Entities;

public sealed class WeatherForecast
{
	public int Id { get; set; }

	public DateOnly Date { get; set; }

	public string? Summary { get; set; }

	public int TemperatureC { get; set; }
}
namespace FocusTemplate.Admin.Shared;

public sealed record WeatherForecastResponse(DateOnly Date, int TemperatureC, string? Summary)
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
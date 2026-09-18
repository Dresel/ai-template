namespace FocusTemplate.Public.Shared;

public partial record WeatherForecastResponse
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
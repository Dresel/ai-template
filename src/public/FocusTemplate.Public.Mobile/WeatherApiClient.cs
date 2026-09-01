using FocusTemplate.Public.Shared;

namespace FocusTemplate.Public.Mobile;

public sealed class WeatherApiClient(HttpClient httpClient)
{
	public async Task<WeatherForecastResponse[]> GetWeatherAsync(CancellationToken cancellationToken = default) =>
		await httpClient.GetFromJsonAsync<WeatherForecastResponse[]>("weatherforecast", cancellationToken) ?? [];
}
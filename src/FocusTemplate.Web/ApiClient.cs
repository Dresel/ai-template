using FocusTemplate.Shared;

namespace FocusTemplate.Web;

public sealed class ApiClient(HttpClient httpClient)
{
	public async Task<WeatherForecast[]> GetWeatherAsync(CancellationToken cancellationToken = default) =>
		await httpClient.GetFromJsonAsync<WeatherForecast[]>("weatherforecast", cancellationToken) ?? [];
}
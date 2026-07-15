using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Web;

public sealed class ApiClient(HttpClient httpClient)
{
	public async Task<WeatherForecastResponse[]> GetWeatherAsync(CancellationToken cancellationToken = default) =>
		await httpClient.GetFromJsonAsync<WeatherForecastResponse[]>("weatherforecast", cancellationToken) ?? [];
}
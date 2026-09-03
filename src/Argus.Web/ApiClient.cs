using Argus.Shared;

namespace Argus.Web;

public sealed class ApiClient(HttpClient httpClient)
{
	public async Task<TokenDeploymentResponse[]> GetTokenDeploymentsAsync(bool tokensOnly, CancellationToken cancellationToken = default) =>
		await httpClient.GetFromJsonAsync<TokenDeploymentResponse[]>($"tokendeployments?tokensOnly={tokensOnly}", cancellationToken) ?? [];

	public async Task<WeatherForecastResponse[]> GetWeatherAsync(CancellationToken cancellationToken = default) =>
		await httpClient.GetFromJsonAsync<WeatherForecastResponse[]>("weatherforecast", cancellationToken) ?? [];
}
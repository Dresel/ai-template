using Argus.Shared;

namespace Argus.Web;

public sealed class ApiClient(HttpClient httpClient)
{
	public async Task<TokenDeploymentResponse[]> GetTokenDeploymentsAsync(
		bool tokensOnly,
		string? search = null,
		CancellationToken cancellationToken = default)
	{
		string query = $"tokendeployments?tokensOnly={tokensOnly}";

		if (!string.IsNullOrWhiteSpace(search))
		{
			query += $"&search={Uri.EscapeDataString(search.Trim())}";
		}

		return await httpClient.GetFromJsonAsync<TokenDeploymentResponse[]>(query, cancellationToken) ?? [];
	}
}
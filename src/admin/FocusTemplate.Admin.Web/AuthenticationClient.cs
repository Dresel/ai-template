using System.Net;
using System.Text.Json;
using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Web;

public sealed class AuthenticationClient(HttpClient httpClient)
{
	private static readonly JsonSerializerOptions JsonOptions =
		new(JsonSerializerDefaults.Web) { TypeInfoResolver = WebJsonContext.Default, };

	public async Task<UserInfoResponse?> GetUserAsync(CancellationToken cancellationToken = default)
	{
		using HttpResponseMessage response =
			await httpClient.GetAsync("bff/user", cancellationToken).ConfigureAwait(false);

		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			return null;
		}

		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync(JsonOptions.GetTypeInfo<UserInfoResponse>(), cancellationToken)
			.ConfigureAwait(false);
	}
}
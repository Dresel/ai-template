using System.Text.Json;
using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Web;

public sealed class DiagnosticsClient(HttpClient httpClient, JsonSerializerOptions? options = null)
{
	private static readonly JsonSerializerOptions DefaultOptions =
		new(JsonSerializerDefaults.Web) { TypeInfoResolver = WebJsonContext.Default, };

	private readonly JsonSerializerOptions jsonOptions = new(options ?? DefaultOptions);

	public async Task<RequestDiagnostics> GetAsync(CancellationToken cancellationToken = default)
	{
		using HttpResponseMessage response = await httpClient.GetAsync(
				"debug/request",
				HttpCompletionOption.ResponseHeadersRead,
				cancellationToken)
			.ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		return await response.Content
			.ReadFromJsonAsync(this.jsonOptions.GetTypeInfo<RequestDiagnostics>(), cancellationToken)
			.ConfigureAwait(false) ?? throw new HttpRequestException(
			"The response body was empty.",
			null,
			response.StatusCode);
	}
}
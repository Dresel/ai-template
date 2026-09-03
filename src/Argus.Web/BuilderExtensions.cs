using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Argus.Web;

public static class BuilderExtensions
{
	public static async Task AddClientConfigurationAsync(this WebAssemblyHostBuilder builder)
	{
		// ReSharper disable once ShortLivedHttpClient
		using HttpClient configClient = new();
		configClient.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);

		using HttpResponseMessage configResponse = await configClient.GetAsync("client-configuration");
		configResponse.EnsureSuccessStatusCode();

		await using Stream configStream = await configResponse.Content.ReadAsStreamAsync();
		builder.Configuration.AddJsonStream(configStream);
	}
}
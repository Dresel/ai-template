using FocusTemplate.Web.Hosting.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResXGenerator.Registration;

namespace FocusTemplate.Web.Hosting;

public static class WebAssemblyHostBuilderExtensions
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

	// The BFF's session as the client's authentication state
	public static WebAssemblyHostBuilder AddWebDefaults(this WebAssemblyHostBuilder builder)
	{
		builder.Services.AddAuthorizationCore();
		builder.Services.AddCascadingAuthenticationState();
		builder.Services.AddScoped<AuthenticationStateProvider, BffAuthenticationStateProvider>();
		builder.Services.AddHttpClient<AuthenticationClient>(client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

		// This assembly's own registration, internal beside the host's
		builder.Services.UsingResXGenerator();

		return builder;
	}
}
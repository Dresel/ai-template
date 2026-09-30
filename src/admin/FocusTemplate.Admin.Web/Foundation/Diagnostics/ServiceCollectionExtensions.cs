using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace FocusTemplate.Admin.Web.Foundation.Diagnostics;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDiagnostics(
		this IServiceCollection services,
		IWebAssemblyHostEnvironment environment) =>
		services.AddHttpClient<DiagnosticsClient>(client => client.BaseAddress = new Uri(environment.BaseAddress))
			.Services;
}
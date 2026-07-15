using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

public static class ServiceCollectionExtensions
{
	public static IHttpClientBuilder AddProxiedHttpClient<TClient>(
		this IServiceCollection services,
		IWebAssemblyHostEnvironment hostEnvironment,
		string serviceName,
		string apiPrefix = "_api")
		where TClient : class
	{
		ArgumentException.ThrowIfNullOrEmpty(serviceName);

		return services.AddHttpClient<TClient>(client => client.BaseAddress = new Uri(
			new Uri(hostEnvironment.BaseAddress),
			$"{apiPrefix}/{serviceName}/"));
	}
}
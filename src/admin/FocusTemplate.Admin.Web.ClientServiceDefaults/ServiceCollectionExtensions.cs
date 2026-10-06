using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

public static class ServiceCollectionExtensions
{
	public static IHttpClientBuilder AddProxiedHttpClient<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
		this IServiceCollection services,
		IWebAssemblyHostEnvironment hostEnvironment,
		string serviceName,
		string apiPrefix = "_api")
		where TClient : class
	{
		ArgumentException.ThrowIfNullOrEmpty(serviceName);

		IHttpClientBuilder builder = services.AddHttpClient<TClient>(client => client.BaseAddress = new Uri(
				new Uri(hostEnvironment.BaseAddress),
				$"{apiPrefix}/{serviceName}/"))
			.AddHttpMessageHandler(() => new CsrfHeaderHandler())

			// TODO: Show popup login dialog instead of redirecting to login page (use message center service & error boundary)
			.AddHttpMessageHandler(provider => new RedirectToLoginHandler(provider.GetRequiredService<NavigationManager>()))

			// Outside the resilience handler, so it sees what that one gives up on
			.AddHttpMessageHandler(() => new NoAnswerHandler());

		// Transient failures retried for reads only: a create or deactivate sent twice would act twice
		builder.AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());

		return builder;
	}
}
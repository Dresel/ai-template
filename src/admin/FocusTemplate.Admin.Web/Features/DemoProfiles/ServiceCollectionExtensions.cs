using FluentValidation;
using FocusTemplate.Admin.Client.DemoProfiles;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;

namespace FocusTemplate.Admin.Web.Features.DemoProfiles;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDemoProfiles(
		this IServiceCollection services,
		IWebAssemblyHostEnvironment environment)
	{
		services.AddProxiedHttpClient<DemoProfilesClient>(environment, "admin-api");
		services.AddScoped<IValidator<Form>, FormValidator>();

		return services;
	}
}
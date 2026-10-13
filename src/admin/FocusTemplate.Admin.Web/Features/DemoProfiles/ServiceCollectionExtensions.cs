using FluentValidation;
using FocusTemplate.Admin.Client.DemoProfiles;
using FocusTemplate.Names;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;

namespace FocusTemplate.Admin.Web.Features.DemoProfiles;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDemoProfiles(
		this IServiceCollection services,
		IWebAssemblyHostEnvironment environment)
	{
		services.AddProxiedHttpClient<DemoProfilesClient>(environment, ResourceNames.AdminApi);
		services.AddScoped<IValidator<Form>, FormValidator>();

		return services;
	}
}
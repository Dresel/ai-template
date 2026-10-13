using FluentValidation;
using FocusTemplate.Admin.Web.Foundation.Pages;
using FocusTemplate.Weather.Client.Stations;
using FocusTemplate.Weather.Client.WeatherForecasts;
using FocusTemplate.Weather.Web.Stations;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResXGenerator.Registration;

namespace FocusTemplate.Weather.Web;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddWeatherPages(
		this IServiceCollection services,
		IWebAssemblyHostEnvironment environment,
		string apiName)
	{
		services.AddProxiedHttpClient<WeatherForecastsClient>(environment, apiName);
		services.AddProxiedHttpClient<StationsClient>(environment, apiName);

		services.AddScoped<IValidator<Form>, FormValidator>();
		services.AddScoped<ListPageViewModel>();

		services.AddSingleton(new PageAssembly(typeof(ServiceCollectionExtensions).Assembly));

		// This assembly's own registration, internal beside the host's
		return services.UsingResXGenerator();
	}
}
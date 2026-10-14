using FluentValidation;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResXGenerator.Registration;
using SpatialFocus.Weather.Client.Stations;
using SpatialFocus.Weather.Client.WeatherForecasts;
using SpatialFocus.Weather.Web.Stations;
using SpatialFocus.Web.Foundation.Pages;

namespace SpatialFocus.Weather.Web;

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
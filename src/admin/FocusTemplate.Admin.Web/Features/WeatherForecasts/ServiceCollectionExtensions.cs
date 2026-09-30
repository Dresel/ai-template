using FocusTemplate.Admin.Client.WeatherForecasts;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;

namespace FocusTemplate.Admin.Web.Features.WeatherForecasts;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddWeatherForecasts(this IServiceCollection services, IWebAssemblyHostEnvironment environment) =>
		services.AddProxiedHttpClient<WeatherForecastsClient>(environment, "admin-api").Services;
}
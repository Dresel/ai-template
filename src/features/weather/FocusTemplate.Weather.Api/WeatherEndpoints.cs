using FocusTemplate.Weather.Api.Stations;
using FocusTemplate.Weather.Api.WeatherForecasts;
using Microsoft.AspNetCore.Routing;

namespace FocusTemplate.Weather.Api;

public static class WeatherEndpoints
{
	public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapWeatherForecastsEndpoints();
		endpoints.MapStationsEndpoints();

		return endpoints;
	}
}
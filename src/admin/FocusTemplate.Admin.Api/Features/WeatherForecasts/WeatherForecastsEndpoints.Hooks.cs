namespace FocusTemplate.Admin.Api.Features.WeatherForecasts;

public static partial class WeatherForecastsEndpoints
{
	static partial void ConfigureGroup(RouteGroupBuilder group) => group.RequireAuthorization();
}
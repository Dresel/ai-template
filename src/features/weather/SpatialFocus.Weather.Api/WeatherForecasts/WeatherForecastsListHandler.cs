using Mediator;
using Microsoft.EntityFrameworkCore;
using SpatialFocus.Weather.Data;
using SpatialFocus.Weather.Shared;

namespace SpatialFocus.Weather.Api.WeatherForecasts;

public sealed class WeatherForecastsListHandler(ReadOnlyWeatherDbContext dbContext)
	: IQueryHandler<WeatherForecastsListQuery, IReadOnlyList<WeatherForecastResponse>>
{
	public async ValueTask<IReadOnlyList<WeatherForecastResponse>> Handle(
		WeatherForecastsListQuery query,
		CancellationToken cancellationToken) =>
		await dbContext.WeatherForecasts.OrderBy(entity => entity.Date)
			.Select(entity => new WeatherForecastResponse(entity.Id, entity.Date, entity.TemperatureC, entity.Summary))
			.ToArrayAsync(cancellationToken);
}
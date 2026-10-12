using FocusTemplate.Weather.Data;
using FocusTemplate.Weather.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Weather.Api.WeatherForecasts;

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
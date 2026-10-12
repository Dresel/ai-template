using FocusTemplate.Weather.Data;
using FocusTemplate.Weather.Data.Entities;
using FocusTemplate.Weather.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Weather.Api.WeatherForecasts;

public sealed class WeatherForecastsGetHandler(ReadOnlyWeatherDbContext dbContext, IWeatherForecastsLocalizations localizations)
	: IQueryHandler<WeatherForecastsGetQuery, WeatherForecastsGetResult>
{
	public async ValueTask<WeatherForecastsGetResult> Handle(WeatherForecastsGetQuery query, CancellationToken cancellationToken)
	{
		WeatherForecast? entity = await dbContext.WeatherForecasts.SingleOrDefaultAsync(
			forecast => forecast.Id == query.Id,
			cancellationToken);

		return entity is null
			? new NotFound(localizations.NoForecast(query.Id))
			: new WeatherForecastResponse(entity.Id, entity.Date, entity.TemperatureC, entity.Summary);
	}
}
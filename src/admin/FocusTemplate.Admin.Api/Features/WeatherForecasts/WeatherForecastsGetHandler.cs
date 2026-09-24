using FocusTemplate.Admin.Shared;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.WeatherForecasts;

public sealed class WeatherForecastsGetHandler(ReadOnlyAppDbContext dbContext)
	: IQueryHandler<WeatherForecastsGetQuery, WeatherForecastsGetResult>
{
	public async ValueTask<WeatherForecastsGetResult> Handle(
		WeatherForecastsGetQuery query,
		CancellationToken cancellationToken)
	{
		WeatherForecast? entity = await dbContext.WeatherForecasts.SingleOrDefaultAsync(
			forecast => forecast.Id == query.Id,
			cancellationToken);

		return entity is null
			? new NotFound($"No weather forecast with id {query.Id}.")
			: new WeatherForecastResponse(entity.Id, entity.Date, entity.TemperatureC, entity.Summary);
	}
}
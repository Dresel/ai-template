using FocusTemplate.Data;
using FocusTemplate.Public.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Public.Api.Features.WeatherForecasts;

public sealed class WeatherForecastsListHandler(AppDbContext dbContext)
	: IQueryHandler<WeatherForecastsListQuery, IReadOnlyList<WeatherForecastResponse>>
{
	public async ValueTask<IReadOnlyList<WeatherForecastResponse>> Handle(
		WeatherForecastsListQuery query,
		CancellationToken cancellationToken) =>
		await dbContext.WeatherForecasts.OrderBy(entity => entity.Date)
			.Select(entity => new WeatherForecastResponse(entity.Date, entity.TemperatureC, entity.Summary))
			.ToArrayAsync(cancellationToken);
}
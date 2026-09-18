using FocusTemplate.Admin.Shared;
using FocusTemplate.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.WeatherForecasts;

public sealed class WeatherForecastsListHandler(AppDbContext dbContext)
	: IQueryHandler<WeatherForecastsListQuery, IReadOnlyList<WeatherForecastResponse>>
{
	public async ValueTask<IReadOnlyList<WeatherForecastResponse>> Handle(
		WeatherForecastsListQuery query,
		CancellationToken cancellationToken) =>
		await dbContext.WeatherForecasts.OrderBy(entity => entity.Date)
			.Select(entity => new WeatherForecastResponse(entity.Id, entity.Date, entity.TemperatureC, entity.Summary))
			.ToArrayAsync(cancellationToken);
}
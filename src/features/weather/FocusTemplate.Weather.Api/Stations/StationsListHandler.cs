using FocusTemplate.Weather.Data;
using FocusTemplate.Weather.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Weather.Api.Stations;

public sealed class StationsListHandler(ReadOnlyWeatherDbContext dbContext)
	: IQueryHandler<StationsListQuery, IReadOnlyList<StationResponse>>
{
	public async ValueTask<IReadOnlyList<StationResponse>> Handle(StationsListQuery query, CancellationToken cancellationToken) =>
		await dbContext.Stations.OrderBy(station => station.Code)
			.Select(station => new StationResponse(station.Id, station.Code, station.Name, station.OwnerId, station.Owner!.DisplayName))
			.ToArrayAsync(cancellationToken);
}
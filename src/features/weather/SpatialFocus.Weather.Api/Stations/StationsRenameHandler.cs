using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SpatialFocus.Api.Foundation.Authorization;
using SpatialFocus.Data.Foundation.Auditing;
using SpatialFocus.Primitives;
using SpatialFocus.Weather.Data;
using SpatialFocus.Weather.Data.Entities;
using SpatialFocus.Weather.Primitives;
using SpatialFocus.Weather.Shared;

namespace SpatialFocus.Weather.Api.Stations;

public sealed class StationsRenameHandler(
	WeatherDbContext dbContext,
	ICurrentUser currentUser,
	IAuthorizationService authorization,
	IHttpContextAccessor httpContextAccessor,
	IStationsLocalizations localizations) : ICommandHandler<StationsRenameCommand, StationsRenameResult>
{
	public async ValueTask<StationsRenameResult> Handle(StationsRenameCommand command, CancellationToken cancellationToken)
	{
		Station? station = await dbContext.Stations.Include(entity => entity.Owner)
			.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

		if (station is null)
		{
			return new NotFound(localizations.NoStation(command.Id));
		}

		if (station.OwnerId != currentUser.Id && !await MayManageStationsAsync())
		{
			return new Forbidden(localizations.NotYours);
		}

		station.Name = command.Body.Name;
		await dbContext.SaveChangesAsync(cancellationToken);

		return new StationResponse(station.Id, station.Code, station.Name, station.OwnerId, station.Owner!.DisplayName);
	}

	private Task<bool> MayManageStationsAsync() =>
		authorization.HasPermissionAsync(
			httpContextAccessor.HttpContext?.User ??
			throw new InvalidOperationException("A command handler runs inside the request of its command."),
			WeatherPermissions.ManageStations);
}
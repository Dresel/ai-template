using Mediator;
using Microsoft.EntityFrameworkCore;
using SpatialFocus.Admin.UserManagement.Api.Groups;
using SpatialFocus.Api.Foundation.Realtime.Backplane;
using SpatialFocus.Data;
using SpatialFocus.Data.Entities;
using SpatialFocus.Data.Foundation.Auditing;

namespace SpatialFocus.Admin.UserManagement.Api.Users;

public sealed class UsersDeactivateHandler(
	AppDbContext dbContext,
	ICurrentUser currentUser,
	SignalBackplane backplane,
	IUsersLocalizations localizations) : ICommandHandler<UsersDeactivateCommand, UsersDeactivateResult>
{
	public async ValueTask<UsersDeactivateResult> Handle(UsersDeactivateCommand command, CancellationToken cancellationToken)
	{
		User? user = await dbContext.Users.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

		if (user is null)
		{
			return new NotFound(localizations.NoUser(command.Id));
		}

		if (user.Id == currentUser.Id)
		{
			return new Conflict(localizations.CannotDeactivateSelf);
		}

		if (await dbContext.WouldLoseTheLastActiveAdministratorAsync(user.Id, cancellationToken))
		{
			return new Conflict(localizations.LastAdministrator(user.DisplayName));
		}

		user.IsActive = false;
		await dbContext.SaveChangesAsync(cancellationToken);

		backplane.CloseStreamsFor(user.Id);

		return Unit.Value;
	}
}
using Mediator;
using Microsoft.EntityFrameworkCore;
using SpatialFocus.Api.Foundation.Realtime.Backplane;
using SpatialFocus.Data;
using SpatialFocus.Data.Entities;

namespace SpatialFocus.Admin.UserManagement.Api.Users;

public sealed class UsersActivateHandler(AppDbContext dbContext, SignalBackplane backplane, IUsersLocalizations localizations)
	: ICommandHandler<UsersActivateCommand, UsersActivateResult>
{
	public async ValueTask<UsersActivateResult> Handle(UsersActivateCommand command, CancellationToken cancellationToken)
	{
		User? user = await dbContext.Users.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

		if (user is null)
		{
			return new NotFound(localizations.NoUser(command.Id));
		}

		user.IsActive = true;
		await dbContext.SaveChangesAsync(cancellationToken);

		backplane.CloseStreamsFor(user.Id);

		return Unit.Value;
	}
}
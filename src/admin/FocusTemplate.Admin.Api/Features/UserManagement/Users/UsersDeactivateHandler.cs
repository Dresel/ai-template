using FocusTemplate.Admin.Api.Features.UserManagement.Groups;
using FocusTemplate.Data;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Data.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Users;

public sealed class UsersDeactivateHandler(AppDbContext dbContext, ICurrentUser currentUser, IUsersLocalizations localizations)
	: ICommandHandler<UsersDeactivateCommand, UsersDeactivateResult>
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

		return Unit.Value;
	}
}
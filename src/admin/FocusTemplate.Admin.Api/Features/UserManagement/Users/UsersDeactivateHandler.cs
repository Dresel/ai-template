using FocusTemplate.Admin.Api.Features.UserManagement.Groups;
using FocusTemplate.Data;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Data.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Users;

public sealed class UsersDeactivateHandler(AppDbContext dbContext, ICurrentUser currentUser)
	: ICommandHandler<UsersDeactivateCommand, UsersDeactivateResult>
{
	public async ValueTask<UsersDeactivateResult> Handle(
		UsersDeactivateCommand command,
		CancellationToken cancellationToken)
	{
		User? user = await dbContext.Users.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

		if (user is null)
		{
			return new NotFound($"No user with id {command.Id}.");
		}

		if (user.Id == currentUser.Id)
		{
			return new Conflict("Users cannot deactivate themselves.");
		}

		if (await dbContext.WouldLoseTheLastActiveAdministratorAsync(user.Id, cancellationToken))
		{
			return new Conflict($"{user.DisplayName} is the last active administrator.");
		}

		user.IsActive = false;
		await dbContext.SaveChangesAsync(cancellationToken);

		return Unit.Value;
	}
}
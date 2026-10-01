using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.Users;

public sealed class UsersActivateHandler(AppDbContext dbContext)
	: ICommandHandler<UsersActivateCommand, UsersActivateResult>
{
	public async ValueTask<UsersActivateResult> Handle(UsersActivateCommand command, CancellationToken cancellationToken)
	{
		User? user = await dbContext.Users.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);
		if (user is null)
		{
			return new NotFound($"No user with id {command.Id}.");
		}

		user.IsActive = true;
		await dbContext.SaveChangesAsync(cancellationToken);

		return Unit.Value;
	}
}
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.Groups;

public sealed class GroupsRemoveMemberHandler(AppDbContext dbContext)
	: ICommandHandler<GroupsRemoveMemberCommand, GroupsRemoveMemberResult>
{
	public async ValueTask<GroupsRemoveMemberResult> Handle(
		GroupsRemoveMemberCommand command,
		CancellationToken cancellationToken)
	{
		Group? group = await dbContext.Groups
			.Include(entity => entity.Members.Where(member => member.Id == command.UserId))
			.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

		if (group?.Members is not [{ } existingMember,])
		{
			return Unit.Value;
		}

		if (command.Id == WellKnownGroups.Administrators &&
			await dbContext.WouldLoseTheLastActiveAdministratorAsync(command.UserId, cancellationToken))
		{
			return new Conflict("The last active administrator cannot leave the Administrators.");
		}

		group.Members.Remove(existingMember);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Unit.Value;
	}
}
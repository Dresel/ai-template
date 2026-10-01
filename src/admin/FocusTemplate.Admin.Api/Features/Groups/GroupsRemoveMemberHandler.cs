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
		if (group?.Members is not [User member,])
		{
			return Unit.Value;
		}

		if (await ManagedGroups.WouldLoseTheirLastActiveMemberAsync(
				dbContext,
				command.UserId,
				command.Id,
				cancellationToken))
		{
			return new Conflict("The last active member of a managed group cannot leave it.");
		}

		group.Members.Remove(member);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Unit.Value;
	}
}
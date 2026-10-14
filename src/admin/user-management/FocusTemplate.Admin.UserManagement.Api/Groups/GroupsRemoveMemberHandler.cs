using FocusTemplate.Admin.UserManagement.Shared;
using FocusTemplate.Api.Foundation.Realtime.Backplane;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.UserManagement.Api.Groups;

public sealed class GroupsRemoveMemberHandler(AppDbContext dbContext, SignalBackplane backplane, IGroupsLocalizations localizations)
	: ICommandHandler<GroupsRemoveMemberCommand, GroupsRemoveMemberResult>
{
	public async ValueTask<GroupsRemoveMemberResult> Handle(GroupsRemoveMemberCommand command, CancellationToken cancellationToken)
	{
		Group? group = await dbContext.Groups.Include(entity => entity.Members.Where(member => member.Id == command.UserId))
			.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

		if (group?.Members is not [{ } existingMember,])
		{
			return Unit.Value;
		}

		if (command.Id == WellKnownGroups.Administrators &&
			await dbContext.WouldLoseTheLastActiveAdministratorAsync(command.UserId, cancellationToken))
		{
			return new Conflict(localizations.LastAdministratorCannotLeave);
		}

		group.Members.Remove(existingMember);
		await dbContext.SaveChangesAsync(cancellationToken);

		backplane.CloseStreamsFor(command.UserId);
		backplane.Publish(new GroupChanged(command.Id, GroupChange.Updated));

		return Unit.Value;
	}
}
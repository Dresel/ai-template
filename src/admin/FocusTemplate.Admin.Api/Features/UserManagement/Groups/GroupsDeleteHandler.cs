using FocusTemplate.Admin.Api.Realtime.Backplane;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

public sealed class GroupsDeleteHandler(AppDbContext dbContext, SignalBackplane backplane, IGroupsLocalizations localizations)
	: ICommandHandler<GroupsDeleteCommand, GroupsDeleteResult>
{
	public async ValueTask<GroupsDeleteResult> Handle(GroupsDeleteCommand command, CancellationToken cancellationToken)
	{
		Group? group = await dbContext.Groups.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

		if (group is null)
		{
			return Unit.Value;
		}

		if (group.IsManaged)
		{
			return new Conflict(localizations.ManagedCannotBeDeleted(group.Name));
		}

		// Read before the cascade in the database removes its memberships and grants with it
		UserId[] members = await dbContext.GetMemberIdsByGroupIdAsync(command.Id, cancellationToken);
		dbContext.Groups.Remove(group);
		await dbContext.SaveChangesAsync(cancellationToken);

		backplane.CloseStreamsFor(members);
		backplane.Publish(new GroupChanged(command.Id, GroupChange.Deleted));

		return Unit.Value;
	}
}
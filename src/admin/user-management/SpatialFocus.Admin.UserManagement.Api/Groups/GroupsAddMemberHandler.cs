using Mediator;
using Microsoft.EntityFrameworkCore;
using SpatialFocus.Admin.UserManagement.Shared;
using SpatialFocus.Api.Foundation.Realtime.Backplane;
using SpatialFocus.Data;
using SpatialFocus.Data.Entities;

namespace SpatialFocus.Admin.UserManagement.Api.Groups;

public sealed class GroupsAddMemberHandler(AppDbContext dbContext, SignalBackplane backplane, IGroupsLocalizations localizations)
	: ICommandHandler<GroupsAddMemberCommand, GroupsAddMemberResult>
{
	public async ValueTask<GroupsAddMemberResult> Handle(GroupsAddMemberCommand command, CancellationToken cancellationToken)
	{
		Group? group = await dbContext.Groups.Include(group => group.Members.Where(member => member.Id == command.UserId))
			.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

		if (group is null)
		{
			return new NotFound(localizations.NoGroup(command.Id));
		}

		if (group.Members.Count > 0)
		{
			return Unit.Value;
		}

		User? user = await dbContext.Users.SingleOrDefaultAsync(entity => entity.Id == command.UserId, cancellationToken);

		if (user is null)
		{
			return new NotFound(localizations.NoUser(command.UserId));
		}

		group.Members.Add(user);
		await dbContext.SaveChangesAsync(cancellationToken);

		backplane.CloseStreamsFor(command.UserId);
		backplane.Publish(new GroupChanged(command.Id, GroupChange.Updated));

		return Unit.Value;
	}
}
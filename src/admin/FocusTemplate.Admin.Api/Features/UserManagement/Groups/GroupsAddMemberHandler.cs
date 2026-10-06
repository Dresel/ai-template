using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

public sealed class GroupsAddMemberHandler(AppDbContext dbContext)
	: ICommandHandler<GroupsAddMemberCommand, GroupsAddMemberResult>
{
	public async ValueTask<GroupsAddMemberResult> Handle(
		GroupsAddMemberCommand command,
		CancellationToken cancellationToken)
	{
		Group? group = await dbContext.Groups
			.Include(group => group.Members.Where(member => member.Id == command.UserId))
			.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

		if (group is null)
		{
			return new NotFound($"No group with id {command.Id}.");
		}

		if (group.Members.Count > 0)
		{
			return Unit.Value;
		}

		User? user = await dbContext.Users.SingleOrDefaultAsync(
			entity => entity.Id == command.UserId,
			cancellationToken);

		if (user is null)
		{
			return new NotFound($"No user with id {command.UserId}.");
		}

		group.Members.Add(user);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Unit.Value;
	}
}
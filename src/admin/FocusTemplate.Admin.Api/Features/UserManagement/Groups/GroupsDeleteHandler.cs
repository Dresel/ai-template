using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

public sealed class GroupsDeleteHandler(AppDbContext dbContext) : ICommandHandler<GroupsDeleteCommand, GroupsDeleteResult>
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
			return new Conflict($"{group.Name} is managed by the application and cannot be deleted.");
		}

		// Its memberships and grants go with it, by the cascade in the database
		dbContext.Groups.Remove(group);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Unit.Value;
	}
}
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.Groups;

public sealed class GroupsUpdateHandler(AppDbContext dbContext) : ICommandHandler<GroupsUpdateCommand, GroupsUpdateResult>
{
	public async ValueTask<GroupsUpdateResult> Handle(GroupsUpdateCommand command, CancellationToken cancellationToken)
	{
		Group? group = await dbContext.Groups.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);
		if (group is null)
		{
			return new NotFound($"No group with id {command.Id}.");
		}

		if (await dbContext.Groups.AnyAsync(
				entity => entity.Id != command.Id && entity.Name == command.Body.Name,
				cancellationToken))
		{
			return new Conflict($"A group named {command.Body.Name} exists already.");
		}

		group.Name = command.Body.Name;
		group.Description = command.Body.Description;
		await dbContext.SaveChangesAsync(cancellationToken);

		return (await GroupResponses.LoadAsync(dbContext, command.Id, cancellationToken))!;
	}
}
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

public sealed class GroupsUpdateHandler(AppDbContext dbContext, IGroupsLocalizations localizations)
	: ICommandHandler<GroupsUpdateCommand, GroupsUpdateResult>
{
	public async ValueTask<GroupsUpdateResult> Handle(GroupsUpdateCommand command, CancellationToken cancellationToken)
	{
		Group? group = await dbContext.Groups.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);
		if (group is null)
		{
			return new NotFound(localizations.NoGroup(command.Id));
		}

		if (await dbContext.Groups.AnyAsync(entity => entity.Id != command.Id && entity.Name == command.Body.Name, cancellationToken))
		{
			return Taken(command.Body.Name);
		}

		group.Name = command.Body.Name;
		group.Description = command.Body.Description;

		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException exception) when (exception.InnerException is PostgresException
		{
			SqlState: PostgresErrorCodes.UniqueViolation,
		})
		{
			// Another request took the name between the check and the update
			return Taken(command.Body.Name);
		}

		return (await dbContext.GetGroupByIdAsync(command.Id, cancellationToken))!;
	}

	private static Conflict Taken(string name) => new($"A group named {name} exists already.");
}
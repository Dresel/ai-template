using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

public sealed class GroupsCreateHandler(AppDbContext dbContext) : ICommandHandler<GroupsCreateCommand, GroupsCreateResult>
{
	public async ValueTask<GroupsCreateResult> Handle(GroupsCreateCommand command, CancellationToken cancellationToken)
	{
		if (await dbContext.Groups.AnyAsync(group => group.Name == command.Body.Name, cancellationToken))
		{
			return Taken(command.Body.Name);
		}

		Group group = new() { Id = GroupId.New(), Name = command.Body.Name, Description = command.Body.Description, };
		dbContext.Groups.Add(group);

		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException exception) when (exception.InnerException is PostgresException
		{
			SqlState: PostgresErrorCodes.UniqueViolation,
		})
		{
			// Another request took the name between the check and the insert
			return Taken(command.Body.Name);
		}

		return new GroupResponse(group.Id, group.Name, group.IsManaged, 0, [], [], group.Description);
	}

	private static Conflict Taken(string name) => new($"A group named {name} exists already.");
}
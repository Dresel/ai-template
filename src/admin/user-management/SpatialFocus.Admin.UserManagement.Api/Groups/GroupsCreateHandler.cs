using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SpatialFocus.Admin.UserManagement.Shared;
using SpatialFocus.Api.Foundation.Realtime.Backplane;
using SpatialFocus.Data;
using SpatialFocus.Data.Entities;
using SpatialFocus.Primitives;

namespace SpatialFocus.Admin.UserManagement.Api.Groups;

public sealed class GroupsCreateHandler(AppDbContext dbContext, SignalBackplane backplane)
	: ICommandHandler<GroupsCreateCommand, GroupsCreateResult>
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

		backplane.Publish(new GroupChanged(group.Id, GroupChange.Created));

		return new GroupResponse(group.Id, group.Name, group.IsManaged, 0, [], [], group.Description);
	}

	private static Conflict Taken(string name) => new($"A group named {name} exists already.");
}
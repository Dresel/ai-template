using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.Groups;

public sealed class GroupsSetPermissionsHandler(AppDbContext dbContext)
	: ICommandHandler<GroupsSetPermissionsCommand, GroupsSetPermissionsResult>
{
	public async ValueTask<GroupsSetPermissionsResult> Handle(
		GroupsSetPermissionsCommand command,
		CancellationToken cancellationToken)
	{
		Group? group = await dbContext.Groups.Include(entity => entity.Permissions)
			.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);
		if (group is null)
		{
			return new NotFound($"No group with id {command.Id}.");
		}

		if (group.IsManaged)
		{
			return new Conflict($"The permissions of {group.Name} come with each release.");
		}

		HashSet<Permission> requested = [.. command.Permissions,];
		string[] unknown = [.. requested.Except(Permission.All).Select(permission => permission.Value).Order(StringComparer.Ordinal),];
		if (unknown.Length > 0)
		{
			return new Conflict($"Not a permission of this release: {string.Join(", ", unknown)}.");
		}

		group.Permissions.RemoveAll(permission => !requested.Contains(permission.Name));

		HashSet<Permission> missing = [.. requested.Except(group.Permissions.Select(permission => permission.Name)),];
		if (missing.Count > 0)
		{
			group.Permissions.AddRange(
				await dbContext.Permissions.Where(permission => missing.Contains(permission.Name))
					.ToListAsync(cancellationToken));
		}

		await dbContext.SaveChangesAsync(cancellationToken);

		return Unit.Value;
	}
}
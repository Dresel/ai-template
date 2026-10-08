using FocusTemplate.Admin.Api.Streams;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

public sealed class GroupsSetPermissionsHandler(AppDbContext dbContext, SignalHub<GroupChanged> signals, IGroupsLocalizations localizations)
	: ICommandHandler<GroupsSetPermissionsCommand, GroupsSetPermissionsResult>
{
	public async ValueTask<GroupsSetPermissionsResult> Handle(GroupsSetPermissionsCommand command, CancellationToken cancellationToken)
	{
		Group? group = await dbContext.Groups.Include(entity => entity.Permissions)
			.SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

		if (group is null)
		{
			return new NotFound(localizations.NoGroup(command.Id));
		}

		if (group.IsManaged)
		{
			return new Conflict(localizations.ManagedPermissions(group.Name));
		}

		HashSet<Permission> requested = [.. command.Permissions,];
		string[] unknown = [.. requested.Except(Permission.All).Select(permission => permission.Value).Order(StringComparer.Ordinal),];
		if (unknown.Length > 0)
		{
			return new Conflict(localizations.UnknownPermissions(string.Join(", ", unknown)));
		}

		group.Permissions.RemoveAll(permission => !requested.Contains(permission.Name));

		HashSet<Permission> missing = [.. requested.Except(group.Permissions.Select(permission => permission.Name)),];
		if (missing.Count > 0)
		{
			group.Permissions.AddRange(
				await dbContext.Permissions.Where(permission => missing.Contains(permission.Name)).ToListAsync(cancellationToken));
		}

		await dbContext.SaveChangesAsync(cancellationToken);
		signals.Publish(new GroupChanged(command.Id, GroupChange.Updated));

		return Unit.Value;
	}
}
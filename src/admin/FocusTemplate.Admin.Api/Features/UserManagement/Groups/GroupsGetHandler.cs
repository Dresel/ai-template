using FocusTemplate.Data;
using Mediator;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

public sealed class GroupsGetHandler(ReadOnlyAppDbContext dbContext, IGroupsLocalizations localizations)
	: IQueryHandler<GroupsGetQuery, GroupsGetResult>
{
	public async ValueTask<GroupsGetResult> Handle(GroupsGetQuery query, CancellationToken cancellationToken) =>
		await dbContext.GetGroupByIdAsync(query.Id, cancellationToken) is { } group ? group : new NotFound(localizations.NoGroup(query.Id));
}
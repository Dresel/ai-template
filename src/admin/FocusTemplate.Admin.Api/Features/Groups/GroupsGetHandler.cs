using FocusTemplate.Data;
using Mediator;

namespace FocusTemplate.Admin.Api.Features.Groups;

public sealed class GroupsGetHandler(ReadOnlyAppDbContext dbContext) : IQueryHandler<GroupsGetQuery, GroupsGetResult>
{
	public async ValueTask<GroupsGetResult> Handle(GroupsGetQuery query, CancellationToken cancellationToken) =>
		await GroupResponses.LoadAsync(dbContext, query.Id, cancellationToken) is { } group
			? group
			: new NotFound($"No group with id {query.Id}.");
}
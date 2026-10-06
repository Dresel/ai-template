using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Groups;

public sealed class GroupsListHandler(ReadOnlyAppDbContext dbContext)
	: IQueryHandler<GroupsListQuery, IReadOnlyList<GroupSummaryResponse>>
{
	public async ValueTask<IReadOnlyList<GroupSummaryResponse>> Handle(
		GroupsListQuery query,
		CancellationToken cancellationToken) =>
		await dbContext.Groups.OrderBy(group => group.Name)
			.Select(group => new GroupSummaryResponse(
				group.Id,
				group.Name,
				group.IsManaged,
				group.Members.Count,
				group.Description))
			.ToListAsync(cancellationToken);
}
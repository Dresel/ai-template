using FocusTemplate.Admin.Shared;
using FocusTemplate.Data;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.Groups;

// A group as get answers it, and create and update after their change; the base context, so either kind of handler reads it.
public static class GroupResponses
{
	public static async Task<GroupResponse?> LoadAsync(
		AppDbContextBase dbContext,
		GroupId id,
		CancellationToken cancellationToken)
	{
		GroupSummaryResponse? group = await dbContext.Groups.Where(entity => entity.Id == id)
			.Select(entity => new GroupSummaryResponse(
				entity.Id,
				entity.Name,
				entity.IsManaged,
				entity.Members.Count,
				entity.Description))
			.SingleOrDefaultAsync(cancellationToken);

		if (group is null)
		{
			return null;
		}

		List<GroupMemberResponse> members = await dbContext.Groups.Where(entity => entity.Id == id)
			.SelectMany(entity => entity.Members)
			.OrderBy(user => user.DisplayName)
			.ThenBy(user => user.Id)
			.Select(user => new GroupMemberResponse(user.Id, user.DisplayName, user.IsActive))
			.ToListAsync(cancellationToken);

		List<Permission> permissions = await dbContext.Groups.Where(entity => entity.Id == id)
			.SelectMany(entity => entity.Permissions)
			.Select(permission => permission.Name)
			.OrderBy(name => name)
			.ToListAsync(cancellationToken);

		return new GroupResponse(
			group.Id,
			group.Name,
			group.IsManaged,
			group.MemberCount,
			members,
			permissions,
			group.Description);
	}
}
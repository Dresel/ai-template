using FocusTemplate.Admin.Shared;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.Groups;

public static class Queries
{
	extension(AppDbContextBase dbContext)
	{
		public Task<GroupResponse?> GetGroupByIdAsync(GroupId id, CancellationToken cancellationToken) =>
			dbContext.Groups.AsSplitQuery()
				.Where(entity => entity.Id == id)
				.Select(entity => new GroupResponse(
					entity.Id,
					entity.Name,
					entity.IsManaged,
					entity.Members.Count,
					entity.Members.OrderBy(user => user.DisplayName)
						.ThenBy(user => user.Id)
						.Select(user => new GroupMemberResponse(user.Id, user.DisplayName, user.IsActive))
						.ToList(),
					entity.Permissions.OrderBy(permission => permission.Name)
						.Select(permission => permission.Name)
						.ToList(),
					entity.Description))
				.SingleOrDefaultAsync(cancellationToken);

		public Task<bool> WouldLoseTheLastActiveAdministratorAsync(UserId user, CancellationToken cancellationToken) =>
			dbContext.Groups.AnyAsync(
				entity => entity.Id == WellKnownGroups.Administrators &&
					entity.Members.Any(member => member.Id == user && member.IsActive) &&
					!entity.Members.Any(member => member.Id != user && member.IsActive),
				cancellationToken);
	}
}
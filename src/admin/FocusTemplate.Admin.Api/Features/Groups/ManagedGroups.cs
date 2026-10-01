using FocusTemplate.Data;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.Groups;

// A managed group keeps an active member, or nobody could administer the application any more. Checked before a member
// leaves or is deactivated; two admins doing so at the same moment can still empty it, a race the bootstrap job repairs.
public static class ManagedGroups
{
	// In one group, or in any group when none is given
	public static Task<bool> WouldLoseTheirLastActiveMemberAsync(
		AppDbContext dbContext,
		UserId user,
		GroupId? group,
		CancellationToken cancellationToken) =>
		dbContext.Groups.AnyAsync(
			entity => entity.IsManaged && (group == null || entity.Id == group.Value) &&
				entity.Members.Any(member => member.Id == user && member.IsActive) &&
				!entity.Members.Any(member => member.Id != user && member.IsActive),
			cancellationToken);
}
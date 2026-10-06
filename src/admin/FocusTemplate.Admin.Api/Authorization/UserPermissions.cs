using FocusTemplate.Data;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Authorization;

public sealed class UserPermissions(ReadOnlyAppDbContext dbContext, ICurrentUser currentUser)
{
	private IReadOnlyList<Permission>? permissions;

	public async ValueTask<IReadOnlyList<Permission>> GetAsync(CancellationToken cancellationToken) =>
		this.permissions ??= currentUser.IdOrDefault is { } user ? await GetAsync(user, cancellationToken) : [];

	public async Task<IReadOnlyList<Permission>> GetAsync(UserId user, CancellationToken cancellationToken) =>
		await dbContext.Users.Where(entity => entity.Id == user && entity.IsActive)
			.SelectMany(entity => entity.Groups)
			.SelectMany(group => group.Permissions)
			.Select(permission => permission.Name)
			.Distinct()
			.OrderBy(name => name)
			.ToListAsync(cancellationToken);
}
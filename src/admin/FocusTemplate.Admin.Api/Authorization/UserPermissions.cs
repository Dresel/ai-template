using System.Security.Claims;
using FocusTemplate.Admin.Api.Authentication;
using FocusTemplate.Data;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Authorization;

public sealed class UserPermissions(ReadOnlyAppDbContext dbContext)
{
	private IReadOnlyList<Permission>? permissions;

	public static async Task<IReadOnlyList<Permission>> OfAsync(
		AppDbContextBase dbContext,
		UserId user,
		CancellationToken cancellationToken) =>
		await dbContext.Users.Where(entity => entity.Id == user && entity.IsActive)
			.SelectMany(entity => entity.Groups)
			.SelectMany(group => group.Permissions)
			.Select(permission => permission.Name)
			.Distinct()
			.OrderBy(name => name)
			.ToListAsync(cancellationToken);

	public async ValueTask<IReadOnlyList<Permission>> GetAsync(
		ClaimsPrincipal principal,
		CancellationToken cancellationToken) =>
		this.permissions ??= principal.GetUserId() is { } user ? await OfAsync(dbContext, user, cancellationToken) : [];
}
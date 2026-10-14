using Mediator;
using Microsoft.EntityFrameworkCore;
using SpatialFocus.Admin.UserManagement.Api.Authorization;
using SpatialFocus.Admin.UserManagement.Shared;
using SpatialFocus.Data;

namespace SpatialFocus.Admin.UserManagement.Api.Users;

public sealed class UsersGetHandler(ReadOnlyAppDbContext dbContext, UserPermissions permissions, IUsersLocalizations localizations)
	: IQueryHandler<UsersGetQuery, UsersGetResult>
{
	public async ValueTask<UsersGetResult> Handle(UsersGetQuery query, CancellationToken cancellationToken)
	{
		// The activity row is written with the user, so it is there whenever the user is
		var user = await dbContext.Users.Where(entity => entity.Id == query.Id)
			.Select(entity => new
			{
				entity.Id,
				entity.DisplayName,
				entity.Email,
				entity.IsActive,
				entity.FirstSeenAt,
				entity.Activity!.LastSeenAt,
				Groups = entity.Groups.OrderBy(group => group.Name)
					.Select(group => new GroupReferenceResponse(group.Id, group.Name))
					.ToList(),
			})
			.SingleOrDefaultAsync(cancellationToken);

		return user is null
			? new NotFound(localizations.NoUser(query.Id))
			: new UserResponse(
				user.Id,
				user.DisplayName,
				user.IsActive,
				user.LastSeenAt,
				user.FirstSeenAt,
				user.Groups,
				await permissions.GetAsync(query.Id, cancellationToken),
				user.Email);
	}
}
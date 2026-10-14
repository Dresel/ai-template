using FocusTemplate.Admin.UserManagement.Api.Authorization;
using FocusTemplate.Admin.UserManagement.Shared;
using FocusTemplate.Data;
using FocusTemplate.Data.Foundation.Auditing;
using FocusTemplate.Primitives;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.UserManagement.Api.Users;

public sealed class UsersMeHandler(
	ReadOnlyAppDbContext dbContext,
	ICurrentUser currentUser,
	UserPermissions permissions) : IQueryHandler<UsersMeQuery, CurrentUserResponse>
{
	public async ValueTask<CurrentUserResponse> Handle(UsersMeQuery query, CancellationToken cancellationToken)
	{
		UserId id = currentUser.Id;

		string? displayName = await dbContext.Users.Where(user => user.Id == id)
			.Select(user => user.DisplayName)
			.SingleOrDefaultAsync(cancellationToken);

		// Missing when the rows went while the provisioning middleware still remembers the user (a reset database), or when
		// the read-only key points at a replica behind the middleware's write. The id stands in, since the login needs this
		return new CurrentUserResponse(id, displayName ?? id.ToString(), await permissions.GetAsync(cancellationToken));
	}
}
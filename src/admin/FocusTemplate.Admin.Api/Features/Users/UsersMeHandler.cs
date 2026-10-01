using FocusTemplate.Admin.Api.Authorization;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Data;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.Users;

public sealed class UsersMeHandler(ReadOnlyAppDbContext dbContext, ICurrentUser currentUser)
	: IQueryHandler<UsersMeQuery, CurrentUserResponse>
{
	public async ValueTask<CurrentUserResponse> Handle(UsersMeQuery query, CancellationToken cancellationToken)
	{
		UserId id = currentUser.Id;
		string? displayName = await dbContext.Users.Where(user => user.Id == id)
			.Select(user => user.DisplayName)
			.SingleOrDefaultAsync(cancellationToken);

		// The provisioning middleware has written the row before any endpoint runs; the id stands in should it be missing
		return new CurrentUserResponse(
			id,
			displayName ?? id.ToString(),
			await UserPermissions.OfAsync(dbContext, id, cancellationToken));
	}
}
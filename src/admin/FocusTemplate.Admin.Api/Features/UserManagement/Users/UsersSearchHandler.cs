using System.Diagnostics;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.UserManagement.Users;

public sealed class UsersSearchHandler(ReadOnlyAppDbContext dbContext)
	: IQueryHandler<UsersSearchQuery, UsersSearchResult>
{
	public async ValueTask<UsersSearchResult> Handle(UsersSearchQuery query, CancellationToken cancellationToken)
	{
		UserSearchRequest request = query.Body;
		string? pattern = SearchPattern.ContainingOrDefault(request.Search);

		IQueryable<User> matching = dbContext.Users.Where(user => pattern == null ||
			EF.Functions.ILike(user.DisplayName, pattern, SearchPattern.EscapeCharacter) || (user.Email != null &&
				EF.Functions.ILike(user.Email, pattern, SearchPattern.EscapeCharacter)));

		int total = await matching.CountAsync(cancellationToken);

		IOrderedQueryable<User> ordered = (request.Sort, request.Descending == true) switch
		{
			(UserSort.DisplayName, false) => matching.OrderBy(user => user.DisplayName).ThenBy(user => user.Id),
			(UserSort.DisplayName, true) => matching.OrderByDescending(user => user.DisplayName)
				.ThenBy(user => user.Id),
			(UserSort.LastSeenAt, false) => matching.OrderBy(user => user.Activity!.LastSeenAt).ThenBy(user => user.Id),
			(UserSort.LastSeenAt, true) => matching.OrderByDescending(user => user.Activity!.LastSeenAt)
				.ThenBy(user => user.Id),
			(UserSort.Email, false) => matching.OrderBy(user => user.Email == null)
				.ThenBy(user => user.Email)
				.ThenBy(user => user.Id),
			(UserSort.Email, true) => matching.OrderBy(user => user.Email == null)
				.ThenByDescending(user => user.Email)
				.ThenBy(user => user.Id),

			// A sort order the spec added and this switch does not know yet
			_ => throw new UnreachableException($"No order for {request.Sort}."),
		};

		List<UserSummaryResponse> items = await ordered.Skip(request.Skip)
			.Take(request.Top)
			.Select(user => new UserSummaryResponse(
				user.Id,
				user.DisplayName,
				user.IsActive,
				user.Activity!.LastSeenAt,
				user.Email))
			.ToListAsync(cancellationToken);

		return new UserPageResponse(items, total);
	}
}
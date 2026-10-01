using FocusTemplate.Admin.Shared;
using FocusTemplate.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.Features.Users;

// One complete query per order, as the precompiled queries of NativeAOT need; the id breaks ties, so paging is stable.
public sealed class UsersSearchHandler(ReadOnlyAppDbContext dbContext) : IQueryHandler<UsersSearchQuery, UsersSearchResult>
{
	private const int DefaultTop = 25;

	public async ValueTask<UsersSearchResult> Handle(UsersSearchQuery query, CancellationToken cancellationToken)
	{
		// TypeSpec defaults never reach the server, so the handler applies them
		UserSearchRequest request = query.Body;
		int skip = request.Skip ?? 0;
		int top = request.Top ?? DefaultTop;
		string? pattern = request.Search is { Length: > 0, } search ? $"%{Escape(search)}%" : null;

		int total = await dbContext.Users.CountAsync(
			user => pattern == null || EF.Functions.ILike(user.DisplayName, pattern, "\\") ||
				(user.Email != null && EF.Functions.ILike(user.Email, pattern, "\\")),
			cancellationToken);

		List<UserSummaryResponse> items = (request.Sort ?? UserSort.DisplayName, request.Descending == true) switch
		{
			(UserSort.LastSeenAt, false) => await dbContext.Users
				.Where(user => pattern == null || EF.Functions.ILike(user.DisplayName, pattern, "\\") ||
					(user.Email != null && EF.Functions.ILike(user.Email, pattern, "\\")))
				.OrderBy(user => user.Activity!.LastSeenAt)
				.ThenBy(user => user.Id)
				.Skip(skip)
				.Take(top)
				.Select(user => new UserSummaryResponse(user.Id, user.DisplayName, user.IsActive, user.Activity!.LastSeenAt, user.Email))
				.ToListAsync(cancellationToken),
			(UserSort.LastSeenAt, true) => await dbContext.Users
				.Where(user => pattern == null || EF.Functions.ILike(user.DisplayName, pattern, "\\") ||
					(user.Email != null && EF.Functions.ILike(user.Email, pattern, "\\")))
				.OrderByDescending(user => user.Activity!.LastSeenAt)
				.ThenBy(user => user.Id)
				.Skip(skip)
				.Take(top)
				.Select(user => new UserSummaryResponse(user.Id, user.DisplayName, user.IsActive, user.Activity!.LastSeenAt, user.Email))
				.ToListAsync(cancellationToken),
			(_, false) => await dbContext.Users
				.Where(user => pattern == null || EF.Functions.ILike(user.DisplayName, pattern, "\\") ||
					(user.Email != null && EF.Functions.ILike(user.Email, pattern, "\\")))
				.OrderBy(user => user.DisplayName)
				.ThenBy(user => user.Id)
				.Skip(skip)
				.Take(top)
				.Select(user => new UserSummaryResponse(user.Id, user.DisplayName, user.IsActive, user.Activity!.LastSeenAt, user.Email))
				.ToListAsync(cancellationToken),
			(_, true) => await dbContext.Users
				.Where(user => pattern == null || EF.Functions.ILike(user.DisplayName, pattern, "\\") ||
					(user.Email != null && EF.Functions.ILike(user.Email, pattern, "\\")))
				.OrderByDescending(user => user.DisplayName)
				.ThenBy(user => user.Id)
				.Skip(skip)
				.Take(top)
				.Select(user => new UserSummaryResponse(user.Id, user.DisplayName, user.IsActive, user.Activity!.LastSeenAt, user.Email))
				.ToListAsync(cancellationToken),
		};

		return new UserPageResponse(items, total);
	}

	// % and _ are wildcards in LIKE, yet a search for "50%" means the percent sign
	private static string Escape(string text) =>
		text.Replace("\\", "\\\\", StringComparison.Ordinal)
			.Replace("%", "\\%", StringComparison.Ordinal)
			.Replace("_", "\\_", StringComparison.Ordinal);
}
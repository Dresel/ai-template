namespace FocusTemplate.Admin.Shared;

public sealed record UserInfoResponse(IReadOnlyList<UserClaim> Claims);
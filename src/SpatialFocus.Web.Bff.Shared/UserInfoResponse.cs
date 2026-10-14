namespace SpatialFocus.Web.Bff.Shared;

public sealed record UserInfoResponse(IReadOnlyList<UserClaim> Claims);
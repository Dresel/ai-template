using Microsoft.AspNetCore.Http;
using SpatialFocus.Api.Foundation.Authentication;
using SpatialFocus.Data.Foundation.Auditing;
using SpatialFocus.Primitives;

namespace SpatialFocus.Api.Hosting.Authentication;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
	public UserId? IdOrDefault => httpContextAccessor.HttpContext?.User.UserIdOrDefault;
}
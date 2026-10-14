using FocusTemplate.Api.Foundation.Authentication;
using FocusTemplate.Data.Foundation.Auditing;
using FocusTemplate.Primitives;
using Microsoft.AspNetCore.Http;

namespace FocusTemplate.Api.Hosting.Authentication;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
	public UserId? IdOrDefault => httpContextAccessor.HttpContext?.User.UserIdOrDefault;
}
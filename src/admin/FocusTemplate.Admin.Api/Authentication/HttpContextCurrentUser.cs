using FocusTemplate.Api.Foundation.Authentication;
using FocusTemplate.Data.Foundation.Auditing;
using FocusTemplate.Primitives;

namespace FocusTemplate.Admin.Api.Authentication;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
	public UserId? IdOrDefault => httpContextAccessor.HttpContext?.User.UserIdOrDefault;
}
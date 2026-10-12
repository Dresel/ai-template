using System.Security.Claims;
using FocusTemplate.Http;
using FocusTemplate.Primitives;
using Microsoft.AspNetCore.Authorization;

namespace FocusTemplate.Api.Foundation.Authorization;

public static class AuthorizationServiceExtensions
{
	extension(IAuthorizationService authorization)
	{
		public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, Permission permission) =>
			(await authorization.AuthorizeAsync(user, null, new PermissionRequirement(permission))).Succeeded;
	}
}
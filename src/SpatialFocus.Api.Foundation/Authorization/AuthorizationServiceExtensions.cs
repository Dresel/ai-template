using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SpatialFocus.Http;
using SpatialFocus.Primitives;

namespace SpatialFocus.Api.Foundation.Authorization;

public static class AuthorizationServiceExtensions
{
	extension(IAuthorizationService authorization)
	{
		public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, Permission permission) =>
			(await authorization.AuthorizeAsync(user, null, new PermissionRequirement(permission))).Succeeded;
	}
}
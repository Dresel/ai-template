using FocusTemplate.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace FocusTemplate.Admin.UserManagement.Api.Authorization;

public sealed class PermissionRequirementHandler(UserPermissions permissions)
	: AuthorizationHandler<PermissionRequirement>
{
	protected override async Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		PermissionRequirement requirement)
	{
		CancellationToken cancellationToken =
			context.Resource is HttpContext http ? http.RequestAborted : CancellationToken.None;

		if ((await permissions.GetAsync(cancellationToken)).Contains(requirement.Permission))
		{
			context.Succeed(requirement);
		}
	}
}
using FocusTemplate.Admin.Shared;
using Microsoft.AspNetCore.Authorization;

namespace FocusTemplate.Admin.Api.Authorization;

public sealed class PermissionRequirementHandler(UserPermissions permissions)
	: AuthorizationHandler<PermissionRequirement>
{
	protected override async Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		PermissionRequirement requirement)
	{
		CancellationToken cancellationToken =
			context.Resource is HttpContext http ? http.RequestAborted : CancellationToken.None;

		if ((await permissions.GetAsync(context.User, cancellationToken)).Contains(requirement.Permission))
		{
			context.Succeed(requirement);
		}
	}
}
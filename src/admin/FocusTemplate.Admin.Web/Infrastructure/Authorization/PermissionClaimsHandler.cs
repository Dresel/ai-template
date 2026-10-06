using FocusTemplate.Admin.Shared;
using Microsoft.AspNetCore.Authorization;

namespace FocusTemplate.Admin.Web.Infrastructure.Authorization;

public sealed class PermissionClaimsHandler : AuthorizationHandler<PermissionRequirement>
{
	protected override Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		PermissionRequirement requirement)
	{
		if (context.User.Has(requirement.Permission))
		{
			context.Succeed(requirement);
		}

		return Task.CompletedTask;
	}
}
using FocusTemplate.Admin.Shared;
using Microsoft.AspNetCore.Authorization;

namespace FocusTemplate.Admin.Web.Infrastructure.Authorization;

public sealed class PermissionClaimsHandler : AuthorizationHandler<PermissionRequirement>
{
	protected override Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		PermissionRequirement requirement)
	{
		if (context.User.HasClaim(BffClaimTypes.Permission, requirement.Permission.Value))
		{
			context.Succeed(requirement);
		}

		return Task.CompletedTask;
	}
}
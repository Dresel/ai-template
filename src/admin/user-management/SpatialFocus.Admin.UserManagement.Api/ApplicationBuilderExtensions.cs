using Microsoft.AspNetCore.Builder;
using SpatialFocus.Admin.UserManagement.Api.Provisioning;

namespace SpatialFocus.Admin.UserManagement.Api;

public static class ApplicationBuilderExtensions
{
	public static IApplicationBuilder UseUserProvisioning(this IApplicationBuilder app) =>
		app.UseMiddleware<UserProvisioningMiddleware>();
}
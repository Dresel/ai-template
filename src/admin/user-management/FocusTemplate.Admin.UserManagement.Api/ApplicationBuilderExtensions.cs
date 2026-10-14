using FocusTemplate.Admin.UserManagement.Api.Provisioning;
using Microsoft.AspNetCore.Builder;

namespace FocusTemplate.Admin.UserManagement.Api;

public static class ApplicationBuilderExtensions
{
	public static IApplicationBuilder UseUserProvisioning(this IApplicationBuilder app) =>
		app.UseMiddleware<UserProvisioningMiddleware>();
}
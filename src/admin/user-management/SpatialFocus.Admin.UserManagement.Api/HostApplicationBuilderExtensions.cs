using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResXGenerator.Registration;
using SpatialFocus.Admin.UserManagement.Api.Authorization;
using SpatialFocus.Admin.UserManagement.Shared;
using SpatialFocus.Api.Foundation.Realtime;
using SpatialFocus.Data;

namespace SpatialFocus.Admin.UserManagement.Api;

public static class HostApplicationBuilderExtensions
{
	// The spine's contexts come along, since user management alone works on the identity tables. The host registers the
	// signal backplane and the current user, and runs UseUserProvisioning() between authentication and authorization
	public static IHostApplicationBuilder AddUserManagement(
		this IHostApplicationBuilder builder,
		string connectionName,
		string readOnlyConnectionName)
	{
		builder.Services.AddAppDbContextPool(connectionName);
		builder.EnrichNpgsqlDbContext<AppDbContext>();

		builder.Services.AddReadOnlyAppDbContextPool(readOnlyConnectionName);
		builder.EnrichNpgsqlDbContext<ReadOnlyAppDbContext>();

		builder.Services.AddUserManagementEndpoints();
		builder.Services.AddHttpContextAccessor();

		builder.Services.AddScoped<UserPermissions>();
		builder.Services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
		builder.Services.AddMemoryCache();

		builder.Services.AddSignal<GroupChanged>();

		builder.Services.UsingResXGenerator();

		return builder;
	}
}
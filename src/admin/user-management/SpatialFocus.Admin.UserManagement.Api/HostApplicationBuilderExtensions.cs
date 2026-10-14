using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResXGenerator.Registration;
using SpatialFocus.Admin.UserManagement.Api.Authorization;
using SpatialFocus.Admin.UserManagement.Shared;
using SpatialFocus.Api.Foundation.Realtime;
using SpatialFocus.Data;
using SpatialFocus.Primitives;

namespace SpatialFocus.Admin.UserManagement.Api;

public static class HostApplicationBuilderExtensions
{
	// The spine's contexts come along, since user management alone works on the identity tables. The host registers the
	// signal backplane and the current user, runs UseUserProvisioning() between authentication and authorization, and
	// hands over its product's permissions
	public static IHostApplicationBuilder AddUserManagement(
		this IHostApplicationBuilder builder,
		string connectionName,
		string readOnlyConnectionName,
		IReadOnlyList<Permission> permissions)
	{
		builder.Services.AddAppDbContextPool(connectionName);
		builder.EnrichNpgsqlDbContext<AppDbContext>();

		builder.Services.AddReadOnlyAppDbContextPool(readOnlyConnectionName);
		builder.EnrichNpgsqlDbContext<ReadOnlyAppDbContext>();

		builder.Services.AddUserManagementEndpoints();
		builder.Services.AddHttpContextAccessor();

		builder.Services.AddSingleton(new PermissionCatalog(permissions));
		builder.Services.AddScoped<UserPermissions>();
		builder.Services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
		builder.Services.AddMemoryCache();

		builder.Services.AddSignal<GroupChanged>();

		builder.Services.UsingResXGenerator();

		return builder;
	}
}
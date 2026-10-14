using FluentValidation;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResXGenerator.Registration;
using SpatialFocus.Admin.UserManagement.Client.Groups;
using SpatialFocus.Admin.UserManagement.Client.Users;
using SpatialFocus.Admin.UserManagement.Web.Users;
using SpatialFocus.Web.Foundation;
using SpatialFocus.Web.Foundation.Pages;

namespace SpatialFocus.Admin.UserManagement.Web;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddUserManagementPages(
		this IServiceCollection services,
		IWebAssemblyHostEnvironment environment,
		string apiName)
	{
		services.AddProxiedHttpClient<UsersClient>(environment, apiName);
		services.AddProxiedHttpClient<GroupsClient>(environment, apiName);

		services.AddLiveUpdates((GroupsClient groups, CancellationToken cancellationToken) => groups.WatchAsync(cancellationToken));

		services.AddScoped<IValidator<Groups.Form>, Groups.FormValidator>();
		services.AddSingleton<PermissionLabels>();

		services.AddScoped<ListPageViewModel>();
		services.AddScoped<DetailPageViewModel>();
		services.AddScoped<Groups.ListPageViewModel>();
		services.AddScoped<Groups.DetailPageViewModel>();

		services.AddSingleton(new PageAssembly(typeof(ServiceCollectionExtensions).Assembly));

		// This assembly's own registration, internal beside the host's
		return services.UsingResXGenerator();
	}
}
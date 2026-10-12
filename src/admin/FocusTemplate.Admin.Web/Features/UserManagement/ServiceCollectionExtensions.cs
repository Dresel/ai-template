using FluentValidation;
using FocusTemplate.Admin.Client.UserManagement.Groups;
using FocusTemplate.Admin.Client.UserManagement.Users;
using FocusTemplate.Admin.Web.Features.UserManagement.Users;
using FocusTemplate.Admin.Web.Foundation;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;

namespace FocusTemplate.Admin.Web.Features.UserManagement;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddUserManagement(this IServiceCollection services, IWebAssemblyHostEnvironment environment)
	{
		services.AddProxiedHttpClient<UsersClient>(environment, "admin-api");
		services.AddProxiedHttpClient<GroupsClient>(environment, "admin-api");

		services.AddLiveUpdates((GroupsClient groups, CancellationToken cancellationToken) => groups.WatchAsync(cancellationToken));

		services.AddScoped<IValidator<Groups.Form>, Groups.FormValidator>();
		services.AddSingleton<PermissionLabels>();

		services.AddScoped<ListPageViewModel>();
		services.AddScoped<DetailPageViewModel>();
		services.AddScoped<Groups.ListPageViewModel>();
		services.AddScoped<Groups.DetailPageViewModel>();

		return services;
	}
}
using FluentValidation;
using FocusTemplate.Admin.Client.Groups;
using FocusTemplate.Admin.Client.Users;
using FocusTemplate.Admin.Web.Features.UserManagement.Users;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;

namespace FocusTemplate.Admin.Web.Features.UserManagement;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddUserManagement(
		this IServiceCollection services,
		IWebAssemblyHostEnvironment environment)
	{
		services.AddProxiedHttpClient<UsersClient>(environment, "admin-api");
		services.AddProxiedHttpClient<GroupsClient>(environment, "admin-api");
		services.AddScoped<IValidator<Groups.Form>, Groups.FormValidator>();

		services.AddScoped<ListPageViewModel>();
		services.AddScoped<DetailPageViewModel>();
		services.AddScoped<Groups.ListPageViewModel>();
		services.AddScoped<Groups.DetailPageViewModel>();

		return services;
	}
}
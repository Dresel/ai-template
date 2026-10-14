using Microsoft.Extensions.DependencyInjection;
using ResXGenerator.Registration;

namespace FocusTemplate.Admin.Shared.Localization;

public static class ServiceCollectionExtensions
{
	// Both hosts call it beside their own UsingResXGenerator(), since this assembly's registration is internal
	public static IServiceCollection AddAdminSharedLocalization(this IServiceCollection services) =>
		services.UsingResXGenerator();
}
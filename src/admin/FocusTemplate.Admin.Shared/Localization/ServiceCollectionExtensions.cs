using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using ResXGenerator.Registration;

namespace FocusTemplate.Admin.Shared.Localization;

public static class ServiceCollectionExtensions
{
	// Both hosts call it beside their own UsingResXGenerator(), since this assembly's registration is internal
	public static IServiceCollection AddAdminSharedLocalization(this IServiceCollection services)
	{
		// First, since AddLocalization() adds its default factory only when none is registered
		services.AddSingleton<IStringLocalizerFactory, ConventionBasedStringLocalizerFactory>();
		services.AddLocalization();

		return services.UsingResXGenerator();
	}
}
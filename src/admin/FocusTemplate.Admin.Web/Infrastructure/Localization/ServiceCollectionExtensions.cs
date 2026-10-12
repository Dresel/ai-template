using FocusTemplate.Admin.Shared.Localization;
using FocusTemplate.Admin.Web.Foundation;
using ResXGenerator.Registration;

namespace FocusTemplate.Admin.Web.Infrastructure.Localization;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddWebLocalization(this IServiceCollection services)
	{
		services.AddAdminSharedLocalization().AddFoundationLocalization().UsingResXGenerator();
		services.AddSingleton<CultureStorage>();

		ValidationTexts.UseResources();

		return services;
	}
}
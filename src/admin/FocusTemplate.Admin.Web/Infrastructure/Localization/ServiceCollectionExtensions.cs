using FocusTemplate.Admin.Shared.Localization;
using FocusTemplate.Admin.Web.Foundation;
using FocusTemplate.Admin.Web.Foundation.Localization;
using FocusTemplate.Localization;
using ResXGenerator.Registration;

namespace FocusTemplate.Admin.Web.Infrastructure.Localization;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddWebLocalization(this IServiceCollection services)
	{
		services.AddResxLocalization().AddAdminSharedLocalization().AddFoundationLocalization().UsingResXGenerator();
		services.AddCultures(Cultures.Supported);

		ValidationTexts.UseResources();

		return services;
	}
}
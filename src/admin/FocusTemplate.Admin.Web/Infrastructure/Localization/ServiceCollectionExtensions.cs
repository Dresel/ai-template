using FocusTemplate.Admin.Shared.Localization;
using ResXGenerator.Registration;
using SpatialFocus.Localization;
using SpatialFocus.Web.Foundation;
using SpatialFocus.Web.Foundation.Localization;

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
using Microsoft.Extensions.DependencyInjection;

namespace SpatialFocus.Web.Foundation.Appearance;

public static class ServiceCollectionExtensions
{
	// The host loads appearance.js in its page's head and renders AppearanceTheme in App.razor
	public static IServiceCollection AddAppearance(this IServiceCollection services, string lightTheme, string darkTheme) =>
		services.AddSingleton(new AppearanceThemes(lightTheme, darkTheme)).AddSingleton<AppearanceStorage>();
}
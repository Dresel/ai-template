using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTemplate.Admin.Web.Foundation.Localization;

public static class ServiceCollectionExtensions
{
	// The host loads culture.js in its page's head and calls UseStoredCultureAsync before RunAsync
	public static IServiceCollection AddCultures(this IServiceCollection services, IReadOnlyList<CultureInfo> supported) =>
		services.AddSingleton(new SupportedCultures(supported)).AddSingleton<CultureStorage>();
}
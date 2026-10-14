using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace SpatialFocus.Localization;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddResxLocalization(this IServiceCollection services)
	{
		// First, since AddLocalization() adds its default factory only when none is registered
		services.AddSingleton<IStringLocalizerFactory, ConventionBasedStringLocalizerFactory>();

		return services.AddLocalization();
	}
}
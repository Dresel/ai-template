using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FocusTemplate.Admin.Web.Foundation;

public static class ServiceCollectionExtensions
{
	// Long enough to type a few letters, short enough to feel immediate
	private static readonly TimeSpan SearchPause = TimeSpan.FromMilliseconds(300);

	// A warm answer takes milliseconds, and a busy state flashing for those reads as flicker
	private static readonly TimeSpan ShowBusyAfter = TimeSpan.FromMilliseconds(300);

	// Once shown, the busy state stays at least this long, so it does not flicker
	private static readonly TimeSpan ShowBusyAtLeast = TimeSpan.FromMilliseconds(500);

	public static IServiceCollection AddFoundation(this IServiceCollection services)
	{
		services.TryAddSingleton(TimeProvider.System);
		services.AddTransient(provider => new BusyState(
			ShowBusyAfter,
			ShowBusyAtLeast,
			provider.GetRequiredService<TimeProvider>()));
		services.AddTransient(provider => new Debouncer(SearchPause, provider.GetRequiredService<TimeProvider>()));

		return services;
	}
}
using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Web.Foundation.Feedback;
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
		services.AddSingleton<ApiFailureMessages>();
		services.AddTransient(provider => new BusyState(ShowBusyAfter, ShowBusyAtLeast, provider.GetRequiredService<TimeProvider>()));
		services.AddTransient(provider => new Debouncer(SearchPause, provider.GetRequiredService<TimeProvider>()));

		return services;
	}

	// The client comes fresh for every connect, as typed clients of the HttpClientFactory are meant to
	public static IServiceCollection AddLiveUpdates<TClient, T>(
		this IServiceCollection services,
		Func<TClient, CancellationToken, Task<ServerEventReader<T>>> watch)
		where TClient : notnull =>
		services.AddScoped(provider => new LiveUpdates<T>(
			async cancellationToken => await watch(provider.GetRequiredService<TClient>(), cancellationToken),
			provider.GetRequiredService<TimeProvider>(),
			provider.GetRequiredService<ILogger<LiveUpdates<T>>>()));
}
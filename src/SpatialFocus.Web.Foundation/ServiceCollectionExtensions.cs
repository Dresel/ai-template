using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ResXGenerator.Registration;
using SpatialFocus.Http;
using SpatialFocus.Web.Foundation.Authorization;
using SpatialFocus.Web.Foundation.Feedback;

namespace SpatialFocus.Web.Foundation;

public static class ServiceCollectionExtensions
{
	// Long enough to type a few letters, short enough to feel immediate
	private static readonly TimeSpan SearchPause = TimeSpan.FromMilliseconds(300);

	// A warm answer takes milliseconds, and a busy state flashing for those reads as flicker
	private static readonly TimeSpan ShowBusyAfter = TimeSpan.FromMilliseconds(300);

	// Once shown, the busy state stays at least this long, so it does not flicker
	private static readonly TimeSpan ShowBusyAtLeast = TimeSpan.FromMilliseconds(500);

	// The host registers the AuthenticationStateProvider that CurrentUser and the permission handler read
	public static IServiceCollection AddFoundation(this IServiceCollection services)
	{
		services.TryAddSingleton(TimeProvider.System);
		services.AddSingleton<IAuthorizationHandler, PermissionClaimsHandler>();
		services.AddScoped<CurrentUser>();
		services.AddSingleton<ApiFailureMessages>();
		services.AddTransient(provider => new BusyState(ShowBusyAfter, ShowBusyAtLeast, provider.GetRequiredService<TimeProvider>()));
		services.AddTransient(provider => new Debouncer(SearchPause, provider.GetRequiredService<TimeProvider>()));

		return services;
	}

	// The host calls it beside its own UsingResXGenerator(), since this assembly's registration is internal
	public static IServiceCollection AddFoundationLocalization(this IServiceCollection services) =>
		services.UsingResXGenerator();

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
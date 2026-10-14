using Microsoft.Extensions.DependencyInjection;
using SpatialFocus.Api.Foundation.Realtime.Backplane;

namespace SpatialFocus.Api.Foundation.Realtime;

public static class ServiceCollectionExtensions
{
	// The streams of one signal type, and their subscription to it on the backplane
	public static IServiceCollection AddSignal<TSignal>(this IServiceCollection services) =>
		services.AddSingleton<EventStreams<TSignal>>()
			.AddSingleton<IEventStreams>(provider => provider.GetRequiredService<EventStreams<TSignal>>())
			.AddHostedService<BackplaneSubscription<TSignal>>();

	public static IServiceCollection AddSignalBackplane(this IServiceCollection services) =>
		services.AddSingleton<SignalBackplane>().AddHostedService(provider => provider.GetRequiredService<SignalBackplane>());
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SpatialFocus.Data.Foundation;
using SpatialFocus.Data.Foundation.Auditing;

namespace SpatialFocus.Data;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddAppDbContextPool(this IServiceCollection services, string connectionName)
	{
		services.TryAddSingleton<AuditingInterceptor>();

		services.AddDbContextPool<AppDbContext>((provider, options) => options.ConfigureAppDbContext(
			provider.GetRequiredConnectionString(connectionName),
			provider.GetRequiredService<AuditingInterceptor>()));

		return services;
	}

	public static IServiceCollection AddReadOnlyAppDbContextPool(this IServiceCollection services, string connectionName)
	{
		services.AddDbContextPool<ReadOnlyAppDbContext>((provider, options) =>
			options.ConfigureReadOnlyAppDbContext(provider.GetRequiredConnectionString(connectionName)));

		return services;
	}
}
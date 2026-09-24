using FocusTemplate.Data.Auditing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FocusTemplate.Data;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddAppDbContextPool(this IServiceCollection services, string connectionName)
	{
		services.TryAddSingleton<AuditingInterceptor>();

		services.AddDbContextPool<AppDbContext>((provider, options) => options.ConfigureAppDbContext(
			GetRequiredConnectionString(provider, connectionName),
			provider.GetRequiredService<AuditingInterceptor>()));

		return services;
	}

	public static IServiceCollection AddReadOnlyAppDbContextPool(this IServiceCollection services, string connectionName)
	{
		services.AddDbContextPool<ReadOnlyAppDbContext>((provider, options) =>
			options.ConfigureReadOnlyAppDbContext(GetRequiredConnectionString(provider, connectionName)));

		return services;
	}

	private static string GetRequiredConnectionString(IServiceProvider provider, string connectionName) =>
		provider.GetRequiredService<IConfiguration>().GetConnectionString(connectionName) ??
		throw new InvalidOperationException($"Connection string '{connectionName}' not found.");
}
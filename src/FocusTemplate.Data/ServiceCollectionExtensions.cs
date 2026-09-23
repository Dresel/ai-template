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
			provider.GetRequiredService<IConfiguration>().GetConnectionString(connectionName) ??
			throw new InvalidOperationException($"Connection string '{connectionName}' not found."),
			provider.GetRequiredService<AuditingInterceptor>()));

		return services;
	}
}
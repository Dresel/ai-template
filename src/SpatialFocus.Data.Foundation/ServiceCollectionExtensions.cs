using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using SpatialFocus.Data.Foundation.Auditing;

namespace SpatialFocus.Data.Foundation;

public static class ServiceCollectionExtensions
{
	// The host registers TimeProvider and ICurrentUser, which the singleton interceptor reads on every save
	public static IServiceCollection AddFeatureDbContextPool<TContext>(
		this IServiceCollection services,
		string connectionName,
		Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null)
		where TContext : DbContext, IHasSchema
	{
		services.TryAddSingleton<AuditingInterceptor>();

		services.AddDbContextPool<TContext>((provider, options) =>
		{
			options.UseFeatureProvider(provider.GetRequiredConnectionString(connectionName), TContext.Schema, configureNpgsql);
			options.AddInterceptors(provider.GetRequiredService<AuditingInterceptor>());
		});

		return services;
	}

	public static IServiceCollection AddReadOnlyFeatureDbContextPool<TContext>(
		this IServiceCollection services,
		string connectionName,
		Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null)
		where TContext : DbContext, IHasSchema, IReadOnlyDbContext
	{
		services.AddDbContextPool<TContext>((provider, options) => options.UseReadOnlyFeatureProvider(
			provider.GetRequiredConnectionString(connectionName),
			TContext.Schema,
			configureNpgsql));

		return services;
	}
}
using FocusTemplate.Data.Foundation.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace FocusTemplate.Data.Foundation;

public static class DbContextOptionsBuilderExtensions
{
	public static DbContextOptionsBuilder<TContext> ConfigureFeatureDbContext<TContext>(
		this DbContextOptionsBuilder<TContext> options,
		string connectionString,
		AuditingInterceptor auditing,
		Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null)
		where TContext : DbContext, IHasSchema
	{
		options.UseFeatureProvider(connectionString, TContext.Schema, configureNpgsql);
		options.AddInterceptors(auditing);

		return options;
	}

	public static DbContextOptionsBuilder<TContext> ConfigureReadOnlyFeatureDbContext<TContext>(
		this DbContextOptionsBuilder<TContext> options,
		string connectionString,
		Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null)
		where TContext : DbContext, IHasSchema, IReadOnlyDbContext
	{
		options.UseReadOnlyFeatureProvider(connectionString, TContext.Schema, configureNpgsql);

		return options;
	}

	public static TBuilder UseNpgsqlWithConventions<TBuilder>(
		this TBuilder options,
		string connectionString,
		Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null)
		where TBuilder : DbContextOptionsBuilder
	{
		options.UseNpgsql(connectionString, configureNpgsql);

		options.UseSnakeCaseNamingConvention();
		options.UseValidationCheckConstraints();

		return options;
	}

	// A feature's context keeps its migration history in its own schema, beside its tables
	internal static void UseFeatureProvider(
		this DbContextOptionsBuilder options,
		string connectionString,
		string schema,
		Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql) =>
		options.UseNpgsqlWithConventions(
			connectionString,
			npgsql =>
			{
				npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema);
				configureNpgsql?.Invoke(npgsql);
			});

	internal static void UseReadOnlyFeatureProvider(
		this DbContextOptionsBuilder options,
		string connectionString,
		string schema,
		Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql)
	{
		options.UseFeatureProvider(connectionString, schema, configureNpgsql);
		options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
	}
}
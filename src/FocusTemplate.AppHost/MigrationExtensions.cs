using Aspire.Hosting.EntityFrameworkCore;
using FocusTemplate.Data.Foundation;

namespace FocusTemplate.AppHost;

// See https://aspire.dev/integrations/databases/efcore/migrations/
internal static class MigrationExtensions
{
	public static IResourceBuilder<EFMigrationResource> AddMigrations<TContext>(
		this IResourceBuilder<ProjectResource> startupProject,
		string name,
		string migrationsProjectPath,
		IResourceBuilder<IResourceWithConnectionString> database) =>
		startupProject.AddEFMigrations(name, typeof(TContext).FullName!, ConfigureTool)

			// A path because the data projects' references keep ASPIRE004 quiet with IsAspireProjectResource="false", which
			// also means no Projects type for the typed overload. Switch once ASPIRE004 can be disabled per reference:
			// https://github.com/microsoft/aspire/issues/16876
			.WithMigrationsProject(migrationsProjectPath)

			// The published bundle reads the database under its own name, a design-time factory under the host-neutral one
			.WithReference(database)
			.WithReference(database, DesignTime.ConnectionName)
			.WaitFor(database)
			.RunDatabaseUpdateOnStart()
			.PublishAsMigrationBundle(publishContainer: true);

	private static void ConfigureTool<TTool>(IResourceBuilder<TTool> tool)
		where TTool : IResourceWithEnvironment
	{
		// Workaround for https://github.com/dotnet/sdk/issues/54386: the .NET 11 CLI exports its own telemetry to the inherited OTEL_* endpoint and
		// blocks each `dotnet` command's exit for ~1 min when nothing listens (e.g. E2E runs without the dashboard).
		tool.WithEnvironment("DOTNET_CLI_TELEMETRY_OPTOUT", "true");

		// Seed only in run mode: the published migration bundle runs against real databases.
		if (tool.ApplicationBuilder.ExecutionContext.IsRunMode)
		{
			tool.WithEnvironment(DesignTime.SeedTestDataVariable, bool.TrueString);
		}
	}
}
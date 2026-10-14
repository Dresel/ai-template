using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.EntityFrameworkCore;
using SpatialFocus.Names;

namespace SpatialFocus.AppHost.Hosting.Migrations;

// See https://aspire.dev/integrations/databases/efcore/migrations/
public static class ProjectResourceBuilderExtensions
{
	// The context by its full name and the project by its path, so the AppHost compiles no data project
	public static IResourceBuilder<EFMigrationResource> AddMigrations(
		this IResourceBuilder<ProjectResource> startupProject,
		string name,
		string contextTypeName,
		string migrationsProjectPath,
		IResourceBuilder<IResourceWithConnectionString> database) =>
		startupProject.AddEFMigrations(name, contextTypeName, ConfigureTool)
			.WithMigrationsProject(migrationsProjectPath)

			// The published bundle reads the database under its own name, a design-time factory under the host-neutral one
			.WithReference(database)
			.WithReference(database, DesignTimeNames.ConnectionName)
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
			tool.WithEnvironment(DesignTimeNames.SeedTestDataVariable, bool.TrueString);
		}
	}
}
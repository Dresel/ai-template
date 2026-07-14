using Aspire.Hosting.EntityFrameworkCore;
using FocusTemplate.AppHost;
using Microsoft.Extensions.Configuration;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
builder.AddLocalSettings();

bool addAnalytics = builder.Configuration.GetValue("Features:Analytics", true);
bool addTlsOffloadingIngress = builder.Configuration.GetValue("Features:TlsOffloadingIngress", true);

IResourceBuilder<PostgresDatabaseResource> focusDb = builder.AddPostgres("postgres").AddDatabase("focusdb");
IResourceBuilder<ProjectResource> api = builder.AddProject<FocusTemplate_Api>("api").WithReference(focusDb).WaitFor(focusDb);

// See https://aspire.dev/integrations/databases/efcore/migrations/
IResourceBuilder<EFMigrationResource> migrations = api.AddEFMigrations(
		"api-migrations",
		"FocusTemplate.Data.AppDbContext",
		tool =>
		{
			// Add seeding when running in run mode (e.g. local development)
			if (builder.ExecutionContext.IsRunMode)
			{
				tool.WithEnvironment("Database__SeedTestData", bool.TrueString);
			}
		})
	.WithMigrationsProject("../FocusTemplate.Data/FocusTemplate.Data.csproj") // See https://github.com/microsoft/aspire/issues/16876
	.WithReference(focusDb)
	.WaitFor(focusDb)
	.RunDatabaseUpdateOnStart()
	.PublishAsMigrationBundle(publishContainer: true);

api.WaitForCompletion(migrations);

IResourceBuilder<ProjectResource> web = builder.AddProject<FocusTemplate_Web_Bff>("bff")
	.ProxyBlazorService(api)
	.ProxyBlazorTelemetry()
	.WaitFor(api);

if (addTlsOffloadingIngress)
{
	// This emulates an external ingress (e.g. kubernetes ingress) with tls offloading
	web.WithTlsOffloadingIngress();
}

if (addAnalytics)
{
	IResourceBuilder<PostgresDatabaseResource> umamiDb = builder.AddPostgres("umami-postgres").WithDataVolume().AddDatabase("umami-db");
	IResourceBuilder<UmamiResource> umami = builder.AddUmami("umami").WithPostgreSQL(umamiDb).WaitFor(umamiDb);

	web.WithUmamiAnalytics(umami, "FocusTemplate Web", "localhost");
}

builder.Build().Run();
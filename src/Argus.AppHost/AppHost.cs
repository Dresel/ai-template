using Argus.AppHost;
using Aspire.Hosting.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
builder.AddLocalSettings();

bool addAnalytics = builder.Configuration.GetValue("Features:Analytics", true);
bool addTlsOffloadingIngress = builder.Configuration.GetValue("Features:TlsOffloadingIngress", true);
bool addIntel = builder.Configuration.GetValue("Features:Intel", false);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres");

if (addIntel)
{
	// The intel archive (deployments, cursors, later the entity graph) must survive restarts;
	// without the flag the template keeps its hermetic fresh-database-per-start behavior.
	postgres.WithDataVolume();
}

IResourceBuilder<PostgresDatabaseResource> argusDb = postgres.AddDatabase("argusdb");
IResourceBuilder<ProjectResource> api = builder.AddProject<Argus_Api>("api").WithReference(argusDb).WaitFor(argusDb);

// See https://aspire.dev/integrations/databases/efcore/migrations/
IResourceBuilder<EFMigrationResource> migrations = api.AddEFMigrations(
		"migrations",
		"Argus.Data.AppDbContext")
	.WithMigrationsProject("../Argus.Data/Argus.Data.csproj") // See https://github.com/microsoft/aspire/issues/16876
	.WithReference(argusDb)
	.WaitFor(argusDb)
	.RunDatabaseUpdateOnStart()
	.PublishAsMigrationBundle(publishContainer: true);

api.WaitForCompletion(migrations);

if (addIntel)
{
	// Chain listener (Robinhood Chain by default - see the worker's appsettings.json for the RPC config)
	IResourceBuilder<ProjectResource> intelWorker =
		builder.AddProject<Argus_Worker>("worker").WithReference(argusDb).WaitFor(argusDb);
	intelWorker.WaitForCompletion(migrations);

	// Keyed RPC endpoints stay out of committed config: appsettings.local.json / user secrets only
	if (builder.Configuration["Intel:Ingest:RpcUrls"] is { Length: > 0 } rpcUrls)
	{
		intelWorker.WithEnvironment("Intel__Ingest__RpcUrls", rpcUrls);
	}
}

IResourceBuilder<ProjectResource> web = builder.AddProject<Argus_Web_Bff>("bff")
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

	web.WithUmamiAnalytics(umami, "Argus Web", "localhost");
}

builder.Build().Run();
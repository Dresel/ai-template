using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.EntityFrameworkCore;
using Aspire.Hosting.Maui;
using FocusTemplate.AppHost;
using Microsoft.Extensions.Configuration;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
builder.AddLocalSettings();

bool addAnalytics = builder.Configuration.GetValue("Features:Analytics", true);
bool addTlsOffloadingIngress = builder.Configuration.GetValue("Features:TlsOffloadingIngress", true);
bool addMobile = builder.Configuration.GetValue("Features:Mobile", false);

IResourceBuilder<PostgresDatabaseResource> focusDb = builder.AddPostgres("postgres").AddDatabase("focusdb");
IResourceBuilder<ProjectResource> api = builder.AddProject<FocusTemplate_Admin_Api>("admin-api").WithReference(focusDb).WaitFor(focusDb);

// See https://aspire.dev/integrations/databases/efcore/migrations/
IResourceBuilder<EFMigrationResource> migrations = api.AddEFMigrations(
		"migrations",
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

IResourceBuilder<ProjectResource> publicApi =
	builder.AddProject<FocusTemplate_Public_Api>("public-api").WithReference(focusDb).WaitFor(focusDb);
publicApi.WaitForCompletion(migrations);

if (addMobile)
{
	// See https://aspire.dev/integrations/dotnet/maui/ - ProjectReferences not supported, must reference the csproj directly.
	IResourceBuilder<MauiProjectResource> mobile = builder.AddMauiProject(
		"mobile",
		"../public/FocusTemplate.Public.Mobile/FocusTemplate.Public.Mobile.csproj");

	// Use dev tunnel to forward from mobile emulators / simulators to localhost
	IResourceBuilder<DevTunnelResource> devTunnel = builder.AddDevTunnel("devtunnel")
		.WithAnonymousAccess()
		.WithReference(publicApi.GetEndpoint("http"));

	IResourceBuilder<MauiAndroidEmulatorResource> androidEmulator = mobile.AddAndroidEmulator()
		.WithOtlpDevTunnel()
		.WithReference(publicApi, devTunnel);

	IResourceBuilder<MauiiOSSimulatorResource> iosSimulator = mobile.AddiOSSimulator()
		.WithOtlpDevTunnel()
		.WithReference(publicApi, devTunnel);

	// Workaround for Aspire.Hosting.Maui 13.5.x.
	// See https://github.com/microsoft/aspire/issues/18724 — remove once fixed upstream
	androidEmulator.WithArgs(context => context.Args.Add("-p:NoBuild=false"));
	iosSimulator.WithArgs(context => context.Args.Add("-p:NoBuild=false"));
}

IResourceBuilder<ProjectResource> web = builder.AddProject<FocusTemplate_Admin_Web_Bff>("admin-bff")
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
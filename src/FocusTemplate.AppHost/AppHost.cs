using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.EntityFrameworkCore;
using Aspire.Hosting.Maui;
using FocusTemplate.AppHost.Hosting;
using FocusTemplate.AppHost.Hosting.Analytics;
using FocusTemplate.AppHost.Hosting.Authentication;
using FocusTemplate.AppHost.Hosting.Ingress;
using FocusTemplate.AppHost.Hosting.Migrations;
using FocusTemplate.Data;
using FocusTemplate.Names;
using FocusTemplate.Weather.Data;
using Microsoft.Extensions.Configuration;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
builder.AddLocalSettings();

bool addAnalytics = builder.Configuration.GetValue("Features:Analytics", true);
bool addTlsOffloadingIngress = builder.Configuration.GetValue("Features:TlsOffloadingIngress", true);
bool addMobile = builder.Configuration.GetValue("Features:Mobile", false);

bool addLocalKeycloak = builder.ExecutionContext.IsRunMode && builder.Configuration.GetValue("Features:LocalKeycloak", true);
bool persistentLocalKeycloak = builder.Configuration.GetValue("Features:PersistentLocalKeycloak", false);
bool persistentDatabase = builder.Configuration.GetValue("Features:PersistentDatabase", false);
bool addChaos = builder.ExecutionContext.IsRunMode && builder.Configuration.GetValue("Features:Chaos", false);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres(ResourceNames.Postgres).WithImage("postgis/postgis").WithImageTag("17-3.5");

if (persistentDatabase)
{
	// No wait for Postgres and the migrations on the next start, and the data survives it. A migration edited after it
	// was applied needs `aspire stop --force --volumes`. The proxy keeps `localhost` answering on ::1 as well (see
	// docs/apphost.md), and the fixed port keeps a database tool's connection valid between starts.
	postgres.WithLifetime(ContainerLifetime.Persistent)
		.WithDataVolume()
		.WithEndpoint(
			"tcp",
			endpoint =>
			{
				endpoint.IsProxied = true;
				endpoint.Port = 15432;
			});
}

IResourceBuilder<PostgresDatabaseResource> focusDb = postgres.AddDatabase(ConnectionNames.Database);

// Backplane (pub/sub) for server-sent event signals between the API's processes
IResourceBuilder<RedisResource> redis = builder.AddRedis(ConnectionNames.Redis);

// The realm keycloak/focus-realm.json imports, which is also where its client, audience and secret are declared
KeycloakRealm realm = addLocalKeycloak
	? builder.AddLocalKeycloakRealm(
		ResourceNames.Keycloak,
		8080,
		"focus",
		ResourceNames.AdminApi,
		ResourceNames.AdminBff,
		persistentLocalKeycloak)
	: builder.AddExternalKeycloakRealm(ResourceNames.Keycloak, ResourceNames.AdminApi, ResourceNames.AdminBff);

IResourceBuilder<ProjectResource> api = builder.AddProject<FocusTemplate_Admin_Api>(ResourceNames.AdminApi)
	.WithReference(focusDb)
	.WithReference(focusDb, ConnectionNames.ReadOnlyDatabase)
	.WithReference(redis)
	.WithKeycloakAudience(realm)
	.WaitFor(focusDb)
	.WaitFor(redis);

api.WithUrl($"{api.GetEndpoint("http")}/scalar", "API reference");

if (addChaos)
{
	// Run mode only, so no published environment ever answers slowly or fails on purpose
	IConfigurationSection chaos = builder.Configuration.GetSection("Chaos");
	api.WithEnvironment("Chaos__LatencyRate", chaos["LatencyRate"])
		.WithEnvironment("Chaos__Latency", chaos["Latency"])
		.WithEnvironment("Chaos__FaultRate", chaos["FaultRate"]);
}

IResourceBuilder<EFMigrationResource> migrations = api.AddMigrations<AppDbContext>(
	"migrations",
	"../FocusTemplate.Data/FocusTemplate.Data.csproj",
	focusDb);

// The weather feature's schema, after the spine's, whose users its stations point at
IResourceBuilder<EFMigrationResource> weatherMigrations = api.AddMigrations<WeatherDbContext>(
		"weather-migrations",
		"../features/weather/FocusTemplate.Weather.Data/FocusTemplate.Weather.Data.csproj",
		focusDb)
	.WaitForCompletion(migrations);

api.WaitForCompletion(migrations).WaitForCompletion(weatherMigrations);

IResourceBuilder<ProjectResource> publicApi = builder.AddProject<FocusTemplate_Public_Api>(ResourceNames.PublicApi)
	.WithReference(focusDb, ConnectionNames.ReadOnlyDatabase)
	.WaitFor(focusDb);

publicApi.WaitForCompletion(weatherMigrations);

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

IResourceBuilder<ProjectResource> web = builder.AddProject<FocusTemplate_Admin_Web_Bff>(ResourceNames.AdminBff)
	.ProxyBlazorService(api)
	.ProxyBlazorTelemetry()
	.WithKeycloakClient(realm)
	.WaitFor(api);

if (addTlsOffloadingIngress)
{
	// This emulates an external ingress (e.g. kubernetes ingress) with tls offloading
	web.WithTlsOffloadingIngress(7770);
}

if (addAnalytics)
{
	IResourceBuilder<PostgresDatabaseResource> umamiDb = builder.AddPostgres("umami-postgres").WithDataVolume().AddDatabase("umami-db");
	IResourceBuilder<UmamiResource> umami = builder.AddUmami("umami").WithPostgreSQL(umamiDb).WaitFor(umamiDb);

	web.WithUmamiAnalytics(umami, "FocusTemplate Web", "localhost");
}

builder.Build().Run();
using FocusTemplate.AppHost;
using Microsoft.Extensions.Configuration;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
builder.AddLocalSettings();

bool addAnalytics = builder.Configuration.GetValue("Features:Analytics", defaultValue: true);
bool addTlsOffloadingIngress = builder.Configuration.GetValue("Features:TlsOffloadingIngress", defaultValue: true);

IResourceBuilder<ProjectResource> api = builder.AddProject<FocusTemplate_Api>("api");

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
	IResourceBuilder<PostgresDatabaseResource> umamiDb = builder.AddPostgres("umami-postgres")
		.WithDataVolume()
		.AddDatabase("umami-db");

	IResourceBuilder<UmamiResource> umami = builder.AddUmami("umami")
		.WithPostgreSQL(umamiDb)
		.WaitFor(umamiDb);

	web.WithUmamiAnalytics(umami, websiteName: "FocusTemplate Web", websiteDomain: "localhost");
}

builder.Build().Run();
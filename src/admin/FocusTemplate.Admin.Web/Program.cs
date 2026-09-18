using Blazorise;
using Blazorise.Icons.Material;
using Blazorise.Material;
using FocusTemplate.Admin.Client.WeatherForecasts;
using FocusTemplate.Admin.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// In WebAssembly, environment variables are injected via JS initializer into MonoConfig.environmentVariables.
// They are available via Environment.GetEnvironmentVariable() but NOT automatically in IConfiguration.
// Service Discovery reads from IConfiguration, so we add environment variables to configuration.builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddEnvironmentVariables();

await builder.AddClientConfigurationAsync();

// Add Aspire service defaults (OpenTelemetry, service discovery, resilience)
builder.AddBlazorClientServiceDefaults(serviceName: "admin-web");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), });
builder.Services.AddProxiedHttpClient<WeatherForecastsClient>(builder.HostEnvironment, "admin-api");

builder.Services
	.AddBlazorise(options => options.Immediate = true)
	.AddMaterialProviders()
	.AddMaterialIcons();

WebAssemblyHost host = builder.Build();

host.UseAnalytics();

// WebAssembly does not support IHostedService, so TelemetryHostedService is never started.
// We must force initialization of MeterProvider and TracerProvider manually.
// See: https://github.com/dotnet/aspire/issues/2816
_ = host.Services.GetService<MeterProvider>();
_ = host.Services.GetService<TracerProvider>();

await host.RunAsync();
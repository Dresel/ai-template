using Argus.Web;
using Blazorise;
using Blazorise.Icons.Material;
using Blazorise.Material;
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
builder.Services.AddProxiedHttpClient<ApiClient>(builder.HostEnvironment, "api");

builder.Services
	.AddBlazorise(options =>
	{
		options.Immediate = true;
		options.ProductToken =
			"CjxRBnF3NQs9UwNxfTY1BlEAc3k1ATxQAHZ8Mwo6bjoNJ2ZdYhBVCCo/DDhbBExERldhE1EvN0xcNm46FD1gSkUHCkxESVFvBl4yK1FBfAYKAiFoVXkNWTU3CDJTPHQAGkR/Xip0HhFIeVQ8bxMBUmtTPApwfjUIAWlvHg9QbEMgfwweSX1YJm8eA0RgUzxiDhlWZ1NZAXF+NTUGPG8CBkRqWDBvHgNEYFM8Yg4ZVmdTWQFxQw9nUy95EhpTcUk0bx4DRGBTPGIOGVZnU1kBcX41NQY8bxUcQH1aKnUWEVp1TTtvHhxKb188b3t/NQgBaWMkOHxwdTp+KnZXDlRbSgsPdQ9oNFwbO3RiIxloJjRsUH0qBzUAbA5/UHkwOldwSBRyIxw0cjg1aHUgSH1FB1I7d0JJXwV0c2E8dTwEZCU7fF19O3V5Pm1rZhN/LR9xa2AZeisXM0w+BgYSZXZqOCx4dQp8a201cyc7RHdUE3s0JGcISikAMn43UEgWQjkAV3tULEkwOT1fSitRKjsuD34iaRcMNG56OwYCITg=";
	})
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
using FocusTemplate.Admin.Web;
using FocusTemplate.Admin.Web.Features.DemoProfiles;
using FocusTemplate.Admin.Web.Features.UserManagement;
using FocusTemplate.Admin.Web.Features.WeatherForecasts;
using FocusTemplate.Admin.Web.Foundation;
using FocusTemplate.Admin.Web.Foundation.Diagnostics;
using FocusTemplate.Admin.Web.Infrastructure.Authentication;
using FocusTemplate.Admin.Web.Infrastructure.Authorization;
using FocusTemplate.Admin.Web.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Radzen;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// In WebAssembly, environment variables are injected via JS initializer into MonoConfig.environmentVariables.
// They are available via Environment.GetEnvironmentVariable() but NOT automatically in IConfiguration.
// Service Discovery reads from IConfiguration, so we add environment variables to configuration.
builder.Configuration.AddEnvironmentVariables();

await builder.AddClientConfigurationAsync();

builder.AddBlazorClientServiceDefaults("admin-web");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), });
builder.Services.AddFoundation();
builder.Services.AddDemoProfiles(builder.HostEnvironment);
builder.Services.AddDiagnostics(builder.HostEnvironment);
builder.Services.AddUserManagement(builder.HostEnvironment);
builder.Services.AddWeatherForecasts(builder.HostEnvironment);

builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionClaimsHandler>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, BffAuthenticationStateProvider>();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddHttpClient<AuthenticationClient>(client =>
	client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddRadzenComponents();

WebAssemblyHost host = builder.Build();

host.UseAnalytics();

// WebAssembly does not support IHostedService, so TelemetryHostedService is never started.
// We must force initialization of MeterProvider and TracerProvider manually.
// See: https://github.com/dotnet/aspire/issues/2816
_ = host.Services.GetService<MeterProvider>();
_ = host.Services.GetService<TracerProvider>();

await host.RunAsync();
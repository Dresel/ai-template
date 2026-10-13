using FocusTemplate.Admin.Web;
using FocusTemplate.Admin.Web.Diagnostics;
using FocusTemplate.Admin.Web.Features.DemoProfiles;
using FocusTemplate.Admin.Web.Features.UserManagement;
using FocusTemplate.Admin.Web.Foundation;
using FocusTemplate.Admin.Web.Foundation.Appearance;
using FocusTemplate.Admin.Web.Infrastructure.Authentication;
using FocusTemplate.Admin.Web.Infrastructure.Configuration;
using FocusTemplate.Admin.Web.Infrastructure.Localization;
using FocusTemplate.Names;
using FocusTemplate.Weather.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;
using Radzen;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The OTLP settings arrive as environment variables through the JS initializer of ClientServiceDefaults, and
// BlazorClientExtensions reads them from IConfiguration, which holds no environment variables by default in WASM.
builder.Configuration.AddEnvironmentVariables();

await builder.AddClientConfigurationAsync();

builder.AddBlazorClientServiceDefaults("admin-web");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), });
builder.Services.AddFoundation();
builder.Services.AddAppearance("software", "software-dark");
builder.Services.AddWebLocalization();
builder.Services.AddDemoProfiles(builder.HostEnvironment);
builder.Services.AddDiagnostics(builder.HostEnvironment);
builder.Services.AddUserManagement(builder.HostEnvironment);

builder.Services.AddWeatherPages(builder.HostEnvironment, ResourceNames.AdminApi);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, BffAuthenticationStateProvider>();
builder.Services.AddHttpClient<AuthenticationClient>(client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddRadzenComponents();

WebAssemblyHost host = builder.Build();

host.UseAnalytics();

await host.UseStoredCultureAsync();

await host.RunAsync();
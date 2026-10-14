using FocusTemplate.Admin.Primitives;
using FocusTemplate.Admin.Web;
using FocusTemplate.Admin.Web.Diagnostics;
using FocusTemplate.Admin.Web.Features.DemoProfiles;
using FocusTemplate.Admin.Web.Infrastructure.Localization;
using FocusTemplate.Names;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;
using Radzen;
using SpatialFocus.Admin.UserManagement.Web;
using SpatialFocus.Weather.Web;
using SpatialFocus.Web.Foundation;
using SpatialFocus.Web.Foundation.Appearance;
using SpatialFocus.Web.Foundation.Localization;
using SpatialFocus.Web.Hosting;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The OTLP settings arrive as environment variables through the JS initializer of ClientServiceDefaults, and
// BlazorClientExtensions reads them from IConfiguration, which holds no environment variables by default in WASM.
builder.Configuration.AddEnvironmentVariables();

await builder.AddClientConfigurationAsync();

builder.AddBlazorClientServiceDefaults("admin-web");

builder.AddWebDefaults();

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), });
builder.Services.AddFoundation();
builder.Services.AddAppearance("software", "software-dark");
builder.Services.AddWebLocalization();
builder.Services.AddDemoProfiles(builder.HostEnvironment);
builder.Services.AddDiagnostics(builder.HostEnvironment);

builder.Services.AddUserManagementPages(builder.HostEnvironment, ResourceNames.AdminApi, AdminPermissions.All);
builder.Services.AddWeatherPages(builder.HostEnvironment, ResourceNames.AdminApi);

builder.Services.AddRadzenComponents();

WebAssemblyHost host = builder.Build();

host.UseAnalytics();

await host.UseStoredCultureAsync();

await host.RunAsync();
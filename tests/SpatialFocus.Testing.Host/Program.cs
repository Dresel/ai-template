using System.Globalization;
using SpatialFocus.Admin.UserManagement.Api;
using SpatialFocus.Api.Foundation.Realtime;
using SpatialFocus.Api.Hosting;
using SpatialFocus.Api.Hosting.Authentication;
using SpatialFocus.Api.Hosting.Chaos;
using SpatialFocus.Api.Hosting.Localization;
using SpatialFocus.Testing.Host;
using SpatialFocus.Weather.Api;

// What a product's Admin API composes of the shared code, in its order, without anything of a product
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddApiDefaults();

builder.AddApiAuthentication();

builder.AddApiLocalization([CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("de"),]);

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

builder.AddUserManagement(ConnectionNames.Database, ConnectionNames.ReadOnlyDatabase, TestHostPermissions.All);

builder.AddWeather(ConnectionNames.Database, ConnectionNames.ReadOnlyDatabase);

builder.AddSignalBackplane(ConnectionNames.Redis);

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseExceptionHandler();

app.UseRequestLocalization();

app.UseUnbufferedEventStreams();

app.UseAuthentication();

app.UseUserProvisioning();

app.UseAuthorization();

app.MapApiReference("user-management");

RouteGroupBuilder endpoints = app.MapGroup(string.Empty).AddChaosFilter(app.Configuration);

endpoints.MapUserManagementEndpoints();

endpoints.MapWeatherEndpoints();

app.Run();
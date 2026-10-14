using FocusTemplate.Admin.Api.Features;
using FocusTemplate.Admin.Api.Features.DemoProfiles;
using FocusTemplate.Admin.Primitives;
using FocusTemplate.Admin.Shared.Localization;
using FocusTemplate.Names;
using ResXGenerator.Registration;
using SpatialFocus.Admin.UserManagement.Api;
using SpatialFocus.Admin.UserManagement.Api.Authorization;
using SpatialFocus.Api.Foundation.Realtime;
using SpatialFocus.Api.Hosting;
using SpatialFocus.Api.Hosting.Authentication;
using SpatialFocus.Api.Hosting.Chaos;
using SpatialFocus.Api.Hosting.Localization;
using SpatialFocus.Primitives;
using SpatialFocus.Weather.Api;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddApiDefaults();

builder.AddApiAuthentication();

builder.AddApiLocalization(Cultures.Supported);
builder.Services.AddAdminSharedLocalization().UsingResXGenerator();

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services.AddAdminEndpoints();
builder.Services.AddDemoProfiles();

builder.AddUserManagement(ConnectionNames.Database, ConnectionNames.ReadOnlyDatabase, AdminPermissions.All);

builder.AddWeather(ConnectionNames.Database, ConnectionNames.ReadOnlyDatabase);

builder.AddSignalBackplane(ConnectionNames.Redis);

WebApplication app = builder.Build();

if (args is [AdministratorBootstrap.Command, "--subject", { } subject,])
{
	await AdministratorBootstrap.PromoteAsync(app.Services, UserId.From(Guid.Parse(subject)), CancellationToken.None);
	return;
}

app.MapDefaultEndpoints();

app.UseExceptionHandler();

app.UseRequestLocalization();

app.UseUnbufferedEventStreams();

app.UseAuthentication();

// Before the authorization, so a user who may not do anything yet still appears in the user list
app.UseUserProvisioning();

app.UseAuthorization();

app.MapApiReference("admin");

RouteGroupBuilder endpoints = app.MapGroup(string.Empty).AddChaosFilter(app.Configuration);

endpoints.MapDemoProfilesEndpoints();
endpoints.MapUserManagementEndpoints();

endpoints.MapWeatherEndpoints();

app.Run();
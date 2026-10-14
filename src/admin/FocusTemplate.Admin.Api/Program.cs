using FocusTemplate.Admin.Api.Features;
using FocusTemplate.Admin.Api.Features.DemoProfiles;
using FocusTemplate.Admin.Shared.Localization;
using FocusTemplate.Admin.UserManagement.Api;
using FocusTemplate.Admin.UserManagement.Api.Authorization;
using FocusTemplate.Api.Foundation.Realtime;
using FocusTemplate.Api.Hosting;
using FocusTemplate.Api.Hosting.Authentication;
using FocusTemplate.Api.Hosting.Chaos;
using FocusTemplate.Api.Hosting.Localization;
using FocusTemplate.Names;
using FocusTemplate.Primitives;
using FocusTemplate.Weather.Api;
using ResXGenerator.Registration;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddApiDefaults();

builder.AddApiAuthentication();

builder.AddApiLocalization(Cultures.Supported);
builder.Services.AddAdminSharedLocalization().UsingResXGenerator();

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services.AddAdminEndpoints();
builder.Services.AddDemoProfiles();

builder.AddUserManagement(ConnectionNames.Database, ConnectionNames.ReadOnlyDatabase);

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
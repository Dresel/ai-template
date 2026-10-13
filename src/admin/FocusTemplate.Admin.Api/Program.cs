using FocusTemplate.Admin.Api.Authentication;
using FocusTemplate.Admin.Api.Authorization;
using FocusTemplate.Admin.Api.Chaos;
using FocusTemplate.Admin.Api.Features;
using FocusTemplate.Admin.Api.Features.DemoProfiles;
using FocusTemplate.Admin.Api.Features.UserManagement;
using FocusTemplate.Admin.Api.Localization;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Api.Foundation.Realtime;
using FocusTemplate.Data;
using FocusTemplate.Names;
using FocusTemplate.Primitives;
using FocusTemplate.Weather.Api;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton(TimeProvider.System);

builder.AddApiAuthentication();

builder.AddApiLocalization();

builder.Services.AddAppDbContextPool(ConnectionNames.Database);
builder.EnrichNpgsqlDbContext<AppDbContext>();

builder.Services.AddReadOnlyAppDbContextPool(ConnectionNames.ReadOnlyDatabase);
builder.EnrichNpgsqlDbContext<ReadOnlyAppDbContext>();

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.RespectRequiredConstructorParameters = true);

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services.AddAdminEndpoints();
builder.Services.AddDemoProfiles();

builder.AddWeather(ConnectionNames.Database, ConnectionNames.ReadOnlyDatabase);

builder.AddRedisClient(ConnectionNames.Redis);
builder.Services.AddSignalBackplane().AddSignal<GroupChanged>();

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
app.UseMiddleware<UserProvisioningMiddleware>();

app.UseAuthorization();

app.MapApiReference("admin");

RouteGroupBuilder endpoints = app.MapGroup(string.Empty).AddChaosFilter(app.Configuration);

endpoints.MapDemoProfilesEndpoints();
endpoints.MapUserManagementEndpoints();

endpoints.MapWeatherEndpoints();

app.Run();
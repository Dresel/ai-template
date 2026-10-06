using FocusTemplate.Admin.Api.Authentication;
using FocusTemplate.Admin.Api.Authorization;
using FocusTemplate.Admin.Api.Chaos;
using FocusTemplate.Admin.Api.Features;
using FocusTemplate.Admin.Api.Features.DemoProfiles;
using FocusTemplate.Admin.Api.Features.UserManagement.Groups;
using FocusTemplate.Admin.Api.Features.UserManagement.Users;
using FocusTemplate.Admin.Api.Features.WeatherForecasts;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Data;
using FocusTemplate.Primitives;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton(TimeProvider.System);

builder.AddApiAuthentication();

builder.Services.AddAppDbContextPool("focusdb");
builder.EnrichNpgsqlDbContext<AppDbContext>();

builder.Services.AddReadOnlyAppDbContextPool("focusdb-readonly");
builder.EnrichNpgsqlDbContext<ReadOnlyAppDbContext>();

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.TypeInfoResolverChain.Insert(0, AdminJsonContext.Default);
	options.SerializerOptions.RespectRequiredConstructorParameters = true;
});

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services.AddAdminValidators();
builder.Services.AddDemoProfiles();

WebApplication app = builder.Build();

if (args is [AdministratorBootstrap.Command, "--subject", { } subject,])
{
	await AdministratorBootstrap.PromoteAsync(app.Services, UserId.From(Guid.Parse(subject)), CancellationToken.None);
	return;
}

app.MapDefaultEndpoints();

app.UseExceptionHandler();

app.UseAuthentication();

// Before the authorization, so a user who may not do anything yet still appears in the user list
app.UseMiddleware<UserProvisioningMiddleware>();

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
	app.MapGet(
		"/openapi/v1.yaml",
		() => Results.File(Path.Combine(AppContext.BaseDirectory, "openapi.yaml"), "application/yaml"));
}

RouteGroupBuilder endpoints = app.MapGroup(string.Empty).AddChaosFilter(app.Configuration);

endpoints.MapDemoProfilesEndpoints();
endpoints.MapGroupsEndpoints();
endpoints.MapUsersEndpoints();
endpoints.MapWeatherForecastsEndpoints();

app.Run();
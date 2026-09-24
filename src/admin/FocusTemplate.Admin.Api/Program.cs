using FocusTemplate.Admin.Api.Authentication;
using FocusTemplate.Admin.Api.Features.WeatherForecasts;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Data;

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

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
	app.MapGet(
		"/openapi/v1.yaml",
		() => Results.File(Path.Combine(AppContext.BaseDirectory, "openapi.yaml"), "application/yaml"));
}

app.MapWeatherForecastsEndpoints();

app.Run();
using FocusTemplate.Data;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Public.Api.Features.WeatherForecasts;
using FocusTemplate.Public.Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(WellKnownUsers.System));

builder.Services.AddAppDbContextPool("focusdb");
builder.EnrichNpgsqlDbContext<AppDbContext>();

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
	options.SerializerOptions.TypeInfoResolverChain.Insert(0, PublicJsonContext.Default));

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
	app.MapGet(
		"/openapi/v1.yaml",
		() => Results.File(Path.Combine(AppContext.BaseDirectory, "openapi.yaml"), "application/yaml"));
}

app.MapWeatherForecastsEndpoints();

app.Run();
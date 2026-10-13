using FocusTemplate.Public.Api.Features.WeatherForecasts;
using FocusTemplate.Public.Shared;
using FocusTemplate.Weather.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton(TimeProvider.System);

// At the moment only read-only access is required for the public API
builder.Services.AddReadOnlyWeatherDbContextPool("focusdb-readonly");
builder.EnrichNpgsqlDbContext<ReadOnlyWeatherDbContext>();

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
	options.SerializerOptions.TypeInfoResolverChain.Insert(0, PublicJsonContext.Default));

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseExceptionHandler();

app.MapOpenApiDocuments();

app.MapWeatherForecastsEndpoints();

app.Run();
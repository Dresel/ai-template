using FocusTemplate.Names;
using FocusTemplate.Public.Api.Features.WeatherForecasts;
using FocusTemplate.Public.Shared;
using SpatialFocus.Api.Hosting;
using SpatialFocus.Weather.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddApiDefaults();

// At the moment only read-only access is required for the public API
builder.Services.AddReadOnlyWeatherDbContextPool(ConnectionNames.ReadOnlyDatabase);
builder.EnrichNpgsqlDbContext<ReadOnlyWeatherDbContext>();

builder.Services.ConfigureHttpJsonOptions(options =>
	options.SerializerOptions.TypeInfoResolverChain.Insert(0, PublicJsonContext.Default));

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseExceptionHandler();

app.MapOpenApiDocuments();

app.MapWeatherForecastsEndpoints();

app.Run();
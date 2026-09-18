using FocusTemplate.Data;
using FocusTemplate.Public.Api.Features.WeatherForecasts;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<AppDbContext>("focusdb");

builder.Services.AddProblemDetails();

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

// Map exceptions to problem details
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
	app.MapGet(
		"/openapi/v1.yaml",
		() => Results.File(Path.Combine(AppContext.BaseDirectory, "openapi.yaml"), "application/yaml"));
}

app.MapWeatherForecastsEndpoints();

app.Run();
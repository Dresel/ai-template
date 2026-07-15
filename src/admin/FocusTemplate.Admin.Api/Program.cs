using FocusTemplate.Admin.Shared;
using FocusTemplate.Data;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<AppDbContext>("focusdb");

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.MapGet(
		"/weatherforecast",
		async (AppDbContext dbContext, CancellationToken cancellationToken) =>
		{
			WeatherForecastResponse[] forecast = await dbContext.WeatherForecasts.OrderBy(entity => entity.Date)
				.Select(entity => new WeatherForecastResponse(entity.Date, entity.TemperatureC, entity.Summary))
				.ToArrayAsync(cancellationToken);

			return forecast;
		})
	.WithName("GetWeatherForecast");

app.Run();
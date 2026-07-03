using FocusTemplate.Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",];

app.MapGet(
		"/weatherforecast",
		() =>
		{
			WeatherForecast[] forecast =
			[
				..Enumerable.Range(1, 5)
					.Select(index => new WeatherForecast(
						DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
						Random.Shared.Next(-20, 55),
						summaries[Random.Shared.Next(summaries.Length)])),
			];

			return forecast;
		})
	.WithName("GetWeatherForecast");

app.Run();
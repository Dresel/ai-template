using Argus.Data;
using Argus.Shared;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<AppDbContext>("argusdb");

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.MapGet(
		"/tokendeployments",
		async (AppDbContext dbContext, CancellationToken cancellationToken, bool tokensOnly = false) =>
		{
			IQueryable<Argus.Data.Entities.TokenDeployment> query = dbContext.TokenDeployments;

			if (tokensOnly)
			{
				query = query.Where(entity => entity.TokenSymbol != null);
			}

			TokenDeploymentResponse[] deployments = await query
				.OrderByDescending(entity => entity.DetectedAt)
				.ThenByDescending(entity => entity.Id)
				.Take(100)
				.Select(entity => new TokenDeploymentResponse(
					entity.ChainId,
					entity.ContractAddress,
					entity.DeployerAddress,
					entity.FactoryAddress,
					entity.LaunchpadName,
					entity.TokenName,
					entity.TokenSymbol,
					entity.TokenDecimals,
					entity.BlockNumber,
					entity.DetectedAt))
				.ToArrayAsync(cancellationToken);

			return deployments;
		})
	.WithName("GetTokenDeployments");

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
using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Client.WeatherForecasts;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;
using Xunit.Sdk;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class WeatherForecastTests(ApiFixture factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task GetWeatherForecastReturnsEmptyWhenNoData()
	{
		WeatherForecastsClient client = new(Factory.CreateClient());
		IReadOnlyList<WeatherForecastResponse>
			forecasts = await client.ListAsync(TestContext.Current.CancellationToken);

		Assert.Empty(forecasts);
	}

	// Arranged out of order on purpose: the handler orders by date, the insertion order must not show through.
	[Fact]
	public async Task GetWeatherForecastReturnsExistingRowsInOrder()
	{
		Station station = await Factory.AddTestStationAsync();

		WeatherForecast[] entities =
		[
			new()
			{
				StationId = station.Id,
				Date = new DateOnly(2026, 1, 2),
				TemperatureC = 5,
				Summary = "Chilly",
			},
			new()
			{
				StationId = station.Id,
				Date = new DateOnly(2026, 1, 1),
				TemperatureC = 12,
				Summary = "Mild",
			},
		];

		await AddAsync(entities);

		WeatherForecastsClient client = new(Factory.CreateClient());
		IReadOnlyList<WeatherForecastResponse>
			forecasts = await client.ListAsync(TestContext.Current.CancellationToken);

		IEnumerable<WeatherForecastResponse> expected = entities.OrderBy(entity => entity.Date)
			.Select(entity => new WeatherForecastResponse(entity.Id, entity.Date, entity.TemperatureC, entity.Summary));

		Assert.Equal(expected, forecasts);
	}

	// Guards the HasSentinel mapping: without it both rows would insert the zero key and the second would collide.
	[Fact]
	public async Task InsertedRowsReceiveDistinctStoreGeneratedIds()
	{
		Station station = await Factory.AddTestStationAsync();
		WeatherForecast first = new()
		{
			StationId = station.Id,
			Date = new DateOnly(2026, 1, 1),
			TemperatureC = 5,
			Summary = "Chilly",
		};
		WeatherForecast second = new()
		{
			StationId = station.Id,
			Date = new DateOnly(2026, 1, 2),
			TemperatureC = 12,
			Summary = "Mild",
		};

		await AddAsync(first, second);

		Assert.NotEqual(WeatherForecastId.Unspecified, first.Id);
		Assert.NotEqual(WeatherForecastId.Unspecified, second.Id);
		Assert.NotEqual(first.Id, second.Id);
	}

	[Fact]
	public async Task GetReturnsTheForecastAsTheSuccessCase()
	{
		Station station = await Factory.AddTestStationAsync();
		WeatherForecast entity = new()
		{
			StationId = station.Id,
			Date = new DateOnly(2026, 1, 1),
			TemperatureC = 12,
			Summary = "Mild",
		};
		await AddAsync(entity);

		WeatherForecastsClient client = new(Factory.CreateClient());
		WeatherForecastsGetResult result = await client.GetAsync(entity.Id, TestContext.Current.CancellationToken);

		WeatherForecastResponse forecast = result switch
		{
			WeatherForecastResponse value => value,
			NotFoundProblem problem => throw new XunitException(
				$"Expected the forecast, got 404: {problem.Problem.Detail}"),
		};

		Assert.Equal(new WeatherForecastResponse(entity.Id, new DateOnly(2026, 1, 1), 12, "Mild"), forecast);
	}

	[Fact]
	public async Task GetOfAMissingIdIsTheNotFoundCaseCarryingTheProblemDetails()
	{
		WeatherForecastsClient client = new(Factory.CreateClient());
		WeatherForecastsGetResult result = await client.GetAsync(
			WeatherForecastId.From(4711),
			TestContext.Current.CancellationToken);

		NotFoundProblem problem = result switch
		{
			NotFoundProblem value => value,
			WeatherForecastResponse forecast => throw new XunitException($"Expected 404, got forecast {forecast.Id}."),
		};

		Assert.Equal(404, problem.Problem.Status);
		Assert.Equal("Not found", problem.Problem.Title);
		Assert.Contains("4711", problem.Problem.Detail, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ResetClearsAllRows()
	{
		Station station = await Factory.AddTestStationAsync();
		await AddAsync(
			new WeatherForecast
			{
				StationId = station.Id,
				Date = new DateOnly(2026, 1, 1),
				TemperatureC = 5,
				Summary = "Chilly",
			});
		await Factory.ResetAsync();

		await using AppDbContext dbContext = Factory.CreateDbContext();
		Assert.False(await dbContext.WeatherForecasts.AnyAsync(TestContext.Current.CancellationToken));
		Assert.False(await dbContext.Stations.AnyAsync(TestContext.Current.CancellationToken));
	}

	private async Task AddAsync(params WeatherForecast[] forecasts)
	{
		await using AppDbContext dbContext = Factory.CreateDbContext();

		dbContext.WeatherForecasts.AddRange(forecasts);
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
	}
}
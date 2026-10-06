using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Client.WeatherForecasts;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Xunit.Sdk;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class WeatherForecastTests(ApiFixture factory) : ApiTestBase(factory)
{
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

		WeatherForecastsClient client = new(Factory.CreateAuthenticatedClient());
		IReadOnlyList<WeatherForecastResponse>
			forecasts = await client.ListAsync(TestContext.Current.CancellationToken);

		IEnumerable<WeatherForecastResponse> expected = entities.OrderBy(entity => entity.Date)
			.Select(entity => new WeatherForecastResponse(entity.Id, entity.Date, entity.TemperatureC, entity.Summary));

		Assert.Equal(expected, forecasts);
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

		WeatherForecastsClient client = new(Factory.CreateAuthenticatedClient());
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
	public async Task GetOfAMissingIdIsNotFound()
	{
		WeatherForecastsClient client = new(Factory.CreateAuthenticatedClient());
		WeatherForecastsGetResult result = await client.GetAsync(
			WeatherForecastId.From(4711),
			TestContext.Current.CancellationToken);

		Assert.True(result is NotFoundProblem, $"Expected NotFoundProblem, got {result}");
	}

	private async Task AddAsync(params WeatherForecast[] forecasts)
	{
		await using AppDbContext dbContext = Factory.CreateDbContext();

		dbContext.WeatherForecasts.AddRange(forecasts);
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
	}
}
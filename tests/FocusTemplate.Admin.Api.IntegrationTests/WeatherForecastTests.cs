using FocusTemplate.Admin.Shared;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class WeatherForecastTests(ApiFixture factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task GetWeatherForecastReturnsEmptyWhenNoData()
	{
		using HttpClient client = Factory.CreateClient();

		WeatherForecastResponse[]? forecasts = await client.GetFromJsonAsync<WeatherForecastResponse[]>(
			new Uri("/weatherforecast", UriKind.Relative),
			TestContext.Current.CancellationToken);

		Assert.NotNull(forecasts);
		Assert.Empty(forecasts);
	}

	[Fact]
	public async Task GetWeatherForecastReturnsExistingRowsInOrder()
	{
		WeatherForecast[] forecasts =
		[
			new() { Date = new DateOnly(2026, 1, 2), TemperatureC = 5, Summary = "Chilly", },
			new() { Date = new DateOnly(2026, 1, 1), TemperatureC = 12, Summary = "Mild", },
		];

		await AddAsync(forecasts);

		using HttpClient client = Factory.CreateClient();

		WeatherForecastResponse[]? response = await client.GetFromJsonAsync<WeatherForecastResponse[]>(
			new Uri("/weatherforecast", UriKind.Relative),
			TestContext.Current.CancellationToken);

		Assert.NotNull(response);

		IEnumerable<WeatherForecastResponse> expectedResponse = forecasts.OrderBy(x => x.Date)
			.Select(x => new WeatherForecastResponse(x.Date, x.TemperatureC, x.Summary));

		Assert.Equal(expectedResponse, response);
	}

	[Fact]
	public async Task ResetClearsAllRows()
	{
		await AddAsync(new WeatherForecast { Date = new DateOnly(2026, 1, 1), TemperatureC = 5, Summary = "Chilly", });
		await Factory.ResetAsync();

		await using AppDbContext dbContext = Factory.CreateDbContext();
		Assert.False(await dbContext.WeatherForecasts.AnyAsync(TestContext.Current.CancellationToken));
	}

	private async Task AddAsync(params WeatherForecast[] forecasts)
	{
		await using AppDbContext dbContext = Factory.CreateDbContext();

		dbContext.WeatherForecasts.AddRange(forecasts);
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
	}
}
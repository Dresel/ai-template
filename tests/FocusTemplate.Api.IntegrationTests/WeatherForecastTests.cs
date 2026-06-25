using System.Net;

namespace FocusTemplate.Api.IntegrationTests;

public sealed class WeatherForecastTests(ApiFixture factory) : IClassFixture<ApiFixture>
{
	[Fact]
	public async Task GetWeatherForecastReturnsOk()
	{
		using HttpClient client = factory.CreateClient();

		using HttpResponseMessage response = await client.GetAsync(
			new Uri("/weatherforecast", UriKind.Relative),
			TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}
}
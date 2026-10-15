using System.Net;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class ApiReferenceTests(ApiFixture factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task TheReferenceOffersTheDocumentOfEveryHostedService()
	{
		using HttpClient client = Factory.CreateClient();

		using HttpResponseMessage response = await client.GetAsync("/scalar/", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
		string page = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		Assert.Contains("admin.yaml", page, StringComparison.Ordinal);
		Assert.Contains("user-management.yaml", page, StringComparison.Ordinal);
		Assert.Contains("weather.yaml", page, StringComparison.Ordinal);
	}
}
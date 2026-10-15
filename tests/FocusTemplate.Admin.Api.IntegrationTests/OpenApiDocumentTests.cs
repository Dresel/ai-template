using System.Net;

namespace FocusTemplate.Admin.Api.IntegrationTests;

// The API serves one document per service it hosts, its own and each feature's, under the name its project ships
public sealed class OpenApiDocumentTests(ApiFixture factory) : ApiTestBase(factory)
{
	[Theory]
	[InlineData("admin", "title: FocusTemplate Admin API")]
	[InlineData("user-management", "title: FocusTemplate User Management API")]
	[InlineData("weather", "title: FocusTemplate Weather API")]
	public async Task EachHostedServiceHasADocumentOfItsOwn(string name, string title)
	{
		using HttpClient client = Factory.CreateClient();

		using HttpResponseMessage response = await client.GetAsync($"/openapi/{name}.yaml", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("application/yaml", response.Content.Headers.ContentType?.MediaType);
		Assert.True(response.Headers.CacheControl?.NoCache, "A changed spec reaches the reference with the next reload.");
		Assert.Contains(title, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
	}
}
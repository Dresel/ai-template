using System.Net;

namespace SpatialFocus.Foundation.IntegrationTests;

// The service defaults serve the documents the host's projects ship into openapi/, under their names alone
public sealed class OpenApiDocumentTests(ApiFixture factory) : ApiTestBase(factory)
{
	// Only the shipped names resolve, so no request reaches the file system with a path of its own
	[Theory]
	[InlineData("v1")]
	[InlineData("public")]
	[InlineData("..%2Fappsettings")]
	public async Task ANameNoHostedServiceShipsIsNotFound(string name)
	{
		using HttpClient client = Factory.CreateClient();

		using HttpResponseMessage response = await client.GetAsync($"/openapi/{name}.yaml", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}
}
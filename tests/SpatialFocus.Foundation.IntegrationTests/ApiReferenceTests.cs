using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SpatialFocus.Foundation.IntegrationTests;

// The switch of the service defaults' API reference, on the documents the host's packages ship
public sealed class ApiReferenceTests(ApiFixture factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task ADeployedStageSwitchesItOnThroughItsConfiguration()
	{
		using WebApplicationFactory<Program> host = Configured("Production", "true");
		using HttpClient client = host.CreateClient();

		Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(client, "/scalar/"));
		Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(client, "/openapi/user-management.yaml"));
	}

	[Theory]
	[InlineData("Development", "false")]
	[InlineData("Production", null)]
	public async Task SwitchedOffNeitherThePageNorADocumentIsServed(string environment, string? enabled)
	{
		using WebApplicationFactory<Program> host = Configured(environment, enabled);
		using HttpClient client = host.CreateClient();

		Assert.Equal(HttpStatusCode.NotFound, await StatusOfAsync(client, "/scalar/"));
		Assert.Equal(HttpStatusCode.NotFound, await StatusOfAsync(client, "/openapi/user-management.yaml"));
	}

	private static async Task<HttpStatusCode> StatusOfAsync(HttpClient client, string path)
	{
		using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);
		return response.StatusCode;
	}

	private WebApplicationFactory<Program> Configured(string environment, string? enabled) =>
		Factory.WithWebHostBuilder(builder =>
		{
			builder.UseEnvironment(environment);
			if (enabled is not null)
			{
				builder.UseSetting("ApiReference:Enabled", enabled);
			}
		});
}
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class ApiReferenceTests(ApiFixture factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task ADeployedStageSwitchesItOnThroughItsConfiguration()
	{
		using WebApplicationFactory<Program> host = Configured("Production", "true");
		using HttpClient client = host.CreateClient();

		Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(client, "/scalar/"));
		Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(client, "/openapi/weather.yaml"));
	}

	[Theory]
	[InlineData("Development", "false")]
	[InlineData("Production", null)]
	public async Task SwitchedOffNeitherThePageNorADocumentIsServed(string environment, string? enabled)
	{
		using WebApplicationFactory<Program> host = Configured(environment, enabled);
		using HttpClient client = host.CreateClient();

		Assert.Equal(HttpStatusCode.NotFound, await StatusOfAsync(client, "/scalar/"));
		Assert.Equal(HttpStatusCode.NotFound, await StatusOfAsync(client, "/openapi/admin.yaml"));
	}

	[Fact]
	public async Task TheReferenceOffersTheDocumentOfEveryHostedService()
	{
		using HttpClient client = Factory.CreateClient();

		using HttpResponseMessage response = await client.GetAsync("/scalar/", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
		string page = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		Assert.Contains("admin.yaml", page, StringComparison.Ordinal);
		Assert.Contains("weather.yaml", page, StringComparison.Ordinal);
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
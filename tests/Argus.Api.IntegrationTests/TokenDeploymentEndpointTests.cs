using Argus.Data;
using Argus.Data.Entities;
using Argus.Shared;

namespace Argus.Api.IntegrationTests;

public sealed class TokenDeploymentEndpointTests(ApiFixture factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task GetTokenDeploymentsReturnsNewestFirst()
	{
		await AddAsync(
			Deployment("0xaaa", "OLD", detectedMinutesAgo: 30),
			Deployment("0xbbb", "NEW", detectedMinutesAgo: 1),
			Deployment("0xccc", tokenSymbol: null, detectedMinutesAgo: 10));

		using HttpClient client = Factory.CreateClient();

		TokenDeploymentResponse[]? response = await client.GetFromJsonAsync<TokenDeploymentResponse[]>(
			new Uri("/tokendeployments", UriKind.Relative),
			TestContext.Current.CancellationToken);

		Assert.NotNull(response);
		string[] contractAddresses = [.. response.Select(deployment => deployment.ContractAddress),];
		Assert.Equal(["0xbbb", "0xccc", "0xaaa",], contractAddresses);
		Assert.Equal("NEW", response[0].TokenSymbol);
		Assert.Equal("pons", response[0].LaunchpadName);
		Assert.Equal(4663, response[0].ChainId);
	}

	[Fact]
	public async Task GetTokenDeploymentsCanFilterToTokensOnly()
	{
		await AddAsync(
			Deployment("0xaaa", "TOK", detectedMinutesAgo: 5),
			Deployment("0xbbb", tokenSymbol: null, detectedMinutesAgo: 1));

		using HttpClient client = Factory.CreateClient();

		TokenDeploymentResponse[]? response = await client.GetFromJsonAsync<TokenDeploymentResponse[]>(
			new Uri("/tokendeployments?tokensOnly=true", UriKind.Relative),
			TestContext.Current.CancellationToken);

		Assert.NotNull(response);
		TokenDeploymentResponse deployment = Assert.Single(response);
		Assert.Equal("0xaaa", deployment.ContractAddress);
	}

	private static TokenDeployment Deployment(string contractAddress, string? tokenSymbol, int detectedMinutesAgo) => new()
	{
		ChainId = 4663,
		BlockNumber = 1000 + detectedMinutesAgo,
		BlockHash = "0xblock",
		TransactionHash = $"0xtx{contractAddress}",
		ContractAddress = contractAddress,
		DeployerAddress = "0xdeployer",
		FactoryAddress = "0xfactory",
		LaunchpadName = tokenSymbol is null ? null : "pons",
		TokenName = tokenSymbol is null ? null : $"{tokenSymbol} Token",
		TokenSymbol = tokenSymbol,
		TokenDecimals = tokenSymbol is null ? null : 18,
		DetectedAt = DateTimeOffset.UtcNow.AddMinutes(-detectedMinutesAgo),
	};

	private async Task AddAsync(params TokenDeployment[] deployments)
	{
		await using AppDbContext dbContext = Factory.CreateDbContext();

		dbContext.TokenDeployments.AddRange(deployments);
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
	}
}
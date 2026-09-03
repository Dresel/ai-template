using System.Globalization;
using System.Numerics;
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

	[Fact]
	public async Task GetTokenDeploymentsExposesLaunchSignals()
	{
		TokenDeployment deployment = Deployment("0xaaa", "KARMA", detectedMinutesAgo: 1);
		deployment.PairTokenAddress = "0x0000000000000000000000000000000000000000";
		deployment.CreatorBuyQuote = BigInteger.Parse("54123711340206195", CultureInfo.InvariantCulture);
		deployment.Insiders =
		[
			new TokenDeploymentInsider { Address = "0xinsider1", },
			new TokenDeploymentInsider { Address = "0xinsider2", },
		];

		await AddAsync(deployment);

		using HttpClient client = Factory.CreateClient();

		TokenDeploymentResponse[]? response = await client.GetFromJsonAsync<TokenDeploymentResponse[]>(
			new Uri("/tokendeployments", UriKind.Relative),
			TestContext.Current.CancellationToken);

		Assert.NotNull(response);
		TokenDeploymentResponse single = Assert.Single(response);
		Assert.Equal("0x0000000000000000000000000000000000000000", single.PairTokenAddress);

		// uint256 exceeds every JSON number type, so the wire contract carries it as a string.
		Assert.Equal("54123711340206195", single.CreatorBuyQuote);
		Assert.Equal(2, single.InsiderCount);
		Assert.Equal("0xtx0xaaa", single.TransactionHash);

		// Pons plus a non-empty whitelist is a T+0 rejection under the t0-v1 rules.
		Assert.Equal(LaunchVerdict.Reject, single.Verdict);
		Assert.Equal("t0-v1", single.RulesVersion);
		Assert.Contains(single.Flags, flag => flag.Code == "insiders" && flag.Severity == LaunchFlagSeverity.Danger);
		Assert.Contains(single.Flags, flag => flag.Code == "launchpad");
		Assert.Contains(single.Flags, flag => flag.Code == "creator-buy" && flag.Label == "creator buy 0.0541 ETH");
	}

	[Theory]
	[InlineData("karma", "0xaaa")]
	[InlineData("KARMA", "0xaaa")]
	[InlineData("founder cat", "0xaaa")]
	[InlineData("0xBBB", "0xbbb")]
	[InlineData("0xdeployer2", "0xbbb")]
	[InlineData("0xtx0xbbb", "0xbbb")]
	public async Task GetTokenDeploymentsSearchesSymbolNameAndAddresses(string search, string expectedContract)
	{
		TokenDeployment karma = Deployment("0xaaa", "KARMA", detectedMinutesAgo: 5);
		karma.TokenName = "Reddit Founder Cat";

		TokenDeployment other = Deployment("0xbbb", "DINO", detectedMinutesAgo: 1);
		other.DeployerAddress = "0xdeployer2";

		await AddAsync(karma, other);

		using HttpClient client = Factory.CreateClient();

		TokenDeploymentResponse[]? response = await client.GetFromJsonAsync<TokenDeploymentResponse[]>(
			new Uri($"/tokendeployments?search={Uri.EscapeDataString(search)}", UriKind.Relative),
			TestContext.Current.CancellationToken);

		Assert.NotNull(response);
		TokenDeploymentResponse single = Assert.Single(response);
		Assert.Equal(expectedContract, single.ContractAddress);
	}

	[Fact]
	public async Task GetTokenDeploymentsIgnoresBlankSearch()
	{
		await AddAsync(Deployment("0xaaa", "TOK", detectedMinutesAgo: 5));

		using HttpClient client = Factory.CreateClient();

		TokenDeploymentResponse[]? response = await client.GetFromJsonAsync<TokenDeploymentResponse[]>(
			new Uri("/tokendeployments?search=%20%20", UriKind.Relative),
			TestContext.Current.CancellationToken);

		Assert.NotNull(response);
		Assert.Single(response);
	}

	private static TokenDeployment Deployment(string contractAddress, string? tokenSymbol, int detectedMinutesAgo) =>
		new()
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
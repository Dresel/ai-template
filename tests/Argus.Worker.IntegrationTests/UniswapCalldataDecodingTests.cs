using System.Globalization;
using System.Numerics;
using Argus.Data;
using Argus.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace Argus.Worker.IntegrationTests;

[Collection(AnvilCollection.Name)]
public sealed class UniswapCalldataDecodingTests(PostgresFixture postgres, AnvilFixture anvil)
{
	private const string InstantLaunchStrategy = "0x23f8209572b4a1C2AD88A42749E830791Fb027f1";

	private const string UniversalRouterStrategy = "0x1242c9439d589cAE85E121B1f79f2aF51e91DCEE";

	private const string NativeQuote = "0x0000000000000000000000000000000000000000";

	/// <summary>
	/// Same minimal factory as the other decoding tests: any call executes an internal CREATE and
	/// ignores its calldata, so a real LiquidityLauncher multicall can ride on the launch transaction.
	/// </summary>
	private const string FactoryInitCode = "0x756960006000a060016000f3600052600a60166000f000" + "6000526016600af3";

	[Fact]
	public async Task LauncherMulticallYieldsStrategyAndCreatorFirstBuy()
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		string connectionString = await this.CreateMigratedDatabaseAsync(cancellationToken);

		Account deployer = new(AnvilFixture.DeployerPrivateKey, AnvilFixture.ChainId);
		Web3 web3 = new(deployer, anvil.RpcUrl);

		TransactionReceipt factoryReceipt = await web3.Eth.TransactionManager.SendTransactionAndWaitForReceiptAsync(
			new TransactionInput { From = deployer.Address, Data = FactoryInitCode, Gas = new HexBigInteger(500_000), },
			cancellationToken);

		using IHost host = this.BuildListenerHost(
			connectionString,
			launchpads: new Dictionary<string, string> { [factoryReceipt.ContractAddress] = "uniswap", });
		await host.StartAsync(cancellationToken);

		try
		{
			BigInteger supply = BigInteger.Parse("1000000000000000000000000000", CultureInfo.InvariantCulture);
			BigInteger creatorBuy = BigInteger.Parse("132271234567890000", CultureInfo.InvariantCulture);

			LiquidityLauncherMulticallFunction multicall = new()
			{
				Data =
				[
					new LauncherCreateTokenFunction
					{
						Factory = deployer.Address,
						Name = "Hookr.fun",
						Symbol = "HOOKR",
						Decimals = 18,
						InitialSupply = supply,
						Recipient = deployer.Address,
						TokenData = [],
					}.GetCallData(),
					new LauncherDistributeTokenFunction
					{
						Token = deployer.Address,
						Distribution =
							new LauncherTokenDistribution
							{
								Strategy = InstantLaunchStrategy, Amount = supply, ConfigData = [],
							},
						Salt = new byte[32],
					}.GetCallData(),
					new LauncherDistributeWithNativeFunction
					{
						Strategy = UniversalRouterStrategy,
						ConfigData = [],
						Salt = new byte[32],
						NativeAmount = creatorBuy,
					}.GetCallData(),
				],
			};

			TransactionReceipt callReceipt = await web3.Eth.TransactionManager.SendTransactionAndWaitForReceiptAsync(
				new TransactionInput
				{
					From = deployer.Address,
					To = factoryReceipt.ContractAddress,
					Data = multicall.GetCallData().ToHex(true),
					Gas = new HexBigInteger(500_000),
				},
				cancellationToken);

			TokenDeployment deployment = await WaitForDeploymentAsync(
				connectionString,
				callReceipt.Logs[0].Address,
				cancellationToken);

			Assert.Equal("uniswap", deployment.LaunchpadName);
			Assert.Equal(InstantLaunchStrategy.ToLowerInvariant(), deployment.LaunchStrategyAddress);
			Assert.Equal(creatorBuy, deployment.CreatorBuyQuote);
			Assert.Equal(NativeQuote, deployment.PairTokenAddress);

			await using AppDbContext dbContext = CreateDbContext(connectionString);
			bool anyInsiders = await dbContext.TokenDeploymentInsiders.AnyAsync(
				entity => entity.TokenDeploymentId == deployment.Id,
				cancellationToken);
			Assert.False(anyInsiders);
		}
		finally
		{
			await host.StopAsync(cancellationToken);
		}
	}

	private static AppDbContext CreateDbContext(string connectionString) =>
		new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);

	private static async Task<TokenDeployment> WaitForDeploymentAsync(
		string connectionString,
		string contractAddress,
		CancellationToken cancellationToken)
	{
		string normalized = contractAddress.ToLowerInvariant();

		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(60));

		while (true)
		{
			try
			{
				await using (AppDbContext dbContext = CreateDbContext(connectionString))
				{
					TokenDeployment? deployment = await dbContext.TokenDeployments.SingleOrDefaultAsync(
						entity => entity.ContractAddress == normalized,
						timeout.Token);

					if (deployment is not null)
					{
						return deployment;
					}
				}

				await Task.Delay(250, timeout.Token);
			}
			catch (OperationCanceledException)
			{
				Assert.Fail($"No TokenDeployment row for contract {normalized} appeared within 60 seconds.");
			}
		}
	}

	private async Task<string> CreateMigratedDatabaseAsync(CancellationToken cancellationToken)
	{
		string connectionString = await postgres.CreateDatabaseAsync($"intel_{Guid.NewGuid():N}");

		await using (AppDbContext dbContext = CreateDbContext(connectionString))
		{
			await dbContext.Database.MigrateAsync(cancellationToken);
		}

		return connectionString;
	}

	private IHost BuildListenerHost(string connectionString, Dictionary<string, string> launchpads)
	{
		HostApplicationBuilder builder = Host.CreateApplicationBuilder();
		builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
		builder.Services.Configure<ChainIngestOptions>(options =>
		{
			options.RpcUrls = anvil.RpcUrl;
			options.Confirmations = 0;
			options.PollInterval = TimeSpan.FromMilliseconds(250);
			options.Launchpads = launchpads;
		});
		builder.Services.AddHostedService<EvmDeploymentListener>();

		return builder.Build();
	}
}
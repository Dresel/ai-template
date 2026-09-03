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
public sealed class PonsCalldataDecodingTests(PostgresFixture postgres, AnvilFixture anvil)
{
	private const string InsiderOne = "0x70997970C51812dc3A010C7d01b50e0d17dc79C8";

	private const string InsiderTwo = "0x3C44CdDdB6a900fa2b585dd299e03d12FA4293BC";

	private const string PairToken = "0x90F79bf6EB2c4f870365E785982E1f101E93b906";

	private const string NativeQuote = "0x0000000000000000000000000000000000000000";

	/// <summary>
	/// Same minimal factory as the detection tests: any call executes an internal CREATE, ignoring
	/// the calldata - which lets the test attach real launchAndBuy calldata to the launch transaction.
	/// </summary>
	private const string FactoryInitCode = "0x756960006000a060016000f3600052600a60166000f000" + "6000526016600af3";

	[Fact]
	public async Task PonsLaunchCalldataYieldsInsiderWhitelistAndCreatorBuy()
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
			launchpads: new Dictionary<string, string> { [factoryReceipt.ContractAddress] = "pons", });
		await host.StartAsync(cancellationToken);

		try
		{
			PonsLaunchAndBuyFunction launch = new()
			{
				Params = CreateTokenParams(deployer.Address),
				LaunchConfigId = 1,
				PairToken = PairToken,
				QuoteIn = new BigInteger(1_230_000),
				MinTokensOut = 0,
				Recipient = deployer.Address,
				SnipeTaxExemptions = [InsiderOne, InsiderTwo,],
			};

			TransactionReceipt callReceipt = await web3.Eth.TransactionManager.SendTransactionAndWaitForReceiptAsync(
				new TransactionInput
				{
					From = deployer.Address,
					To = factoryReceipt.ContractAddress,
					Data = launch.GetCallData().ToHex(true),
					Gas = new HexBigInteger(500_000),
				},
				cancellationToken);

			string childAddress = callReceipt.Logs[0].Address;

			TokenDeployment deployment = await WaitForDeploymentAsync(
				connectionString,
				childAddress,
				cancellationToken);

			Assert.Equal("pons", deployment.LaunchpadName);
			Assert.Equal(PairToken.ToLowerInvariant(), deployment.PairTokenAddress);
			Assert.Equal(new BigInteger(1_230_000), deployment.CreatorBuyQuote);

			await using AppDbContext dbContext = CreateDbContext(connectionString);
			string[] insiders = await dbContext.TokenDeploymentInsiders
				.Where(entity => entity.TokenDeploymentId == deployment.Id)
				.Select(entity => entity.Address)
				.OrderBy(address => address)
				.ToArrayAsync(cancellationToken);

			string[] expected = [InsiderOne.ToLowerInvariant(), InsiderTwo.ToLowerInvariant(),];
			Assert.Equal(expected.OrderBy(address => address), insiders);
		}
		finally
		{
			await host.StopAsync(cancellationToken);
		}
	}

	[Fact]
	public async Task LaunchTokenCalldataYieldsInsidersWithoutCreatorBuy()
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
			launchpads: new Dictionary<string, string> { [factoryReceipt.ContractAddress] = "pons", });
		await host.StartAsync(cancellationToken);

		try
		{
			// PonsV2LaunchFactory.launchToken: the whitelist without a creator buy.
			PonsLaunchTokenFunction launch = new()
			{
				Params = CreateTokenParams(deployer.Address),
				LaunchConfigId = 1,
				PairToken = NativeQuote,
				SnipeTaxExemptions = [InsiderOne,],
			};

			TransactionReceipt callReceipt = await web3.Eth.TransactionManager.SendTransactionAndWaitForReceiptAsync(
				new TransactionInput
				{
					From = deployer.Address,
					To = factoryReceipt.ContractAddress,
					Data = launch.GetCallData().ToHex(true),
					Gas = new HexBigInteger(500_000),
				},
				cancellationToken);

			TokenDeployment deployment = await WaitForDeploymentAsync(
				connectionString,
				callReceipt.Logs[0].Address,
				cancellationToken);

			Assert.Equal(NativeQuote, deployment.PairTokenAddress);
			Assert.Null(deployment.CreatorBuyQuote);

			await using AppDbContext dbContext = CreateDbContext(connectionString);
			string insider = await dbContext.TokenDeploymentInsiders
				.Where(entity => entity.TokenDeploymentId == deployment.Id)
				.Select(entity => entity.Address)
				.SingleAsync(cancellationToken);
			Assert.Equal(InsiderOne.ToLowerInvariant(), insider);
		}
		finally
		{
			await host.StopAsync(cancellationToken);
		}
	}

	/// <summary>
	/// Real Pons launches quote in native ETH, which the calldata carries as the zero address.
	/// That must stay distinguishable from "no launch calldata decoded" (null), because the alert
	/// rules treat an unknown quote asset as a red flag but native as canonical.
	/// </summary>
	[Fact]
	public async Task NativeQuoteLaunchStoresZeroAddressRatherThanNull()
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
			launchpads: new Dictionary<string, string> { [factoryReceipt.ContractAddress] = "pons", });
		await host.StartAsync(cancellationToken);

		try
		{
			PonsLaunchAndBuyFunction launch = new()
			{
				Params = CreateTokenParams(deployer.Address),
				LaunchConfigId = 1,
				PairToken = NativeQuote,
				QuoteIn = new BigInteger(54_123_711_340_206_195),
				MinTokensOut = 0,
				Recipient = deployer.Address,
				SnipeTaxExemptions = [],
			};

			TransactionReceipt callReceipt = await web3.Eth.TransactionManager.SendTransactionAndWaitForReceiptAsync(
				new TransactionInput
				{
					From = deployer.Address,
					To = factoryReceipt.ContractAddress,
					Data = launch.GetCallData().ToHex(true),
					Gas = new HexBigInteger(500_000),
				},
				cancellationToken);

			TokenDeployment deployment = await WaitForDeploymentAsync(
				connectionString,
				callReceipt.Logs[0].Address,
				cancellationToken);

			Assert.Equal(NativeQuote, deployment.PairTokenAddress);
			Assert.Equal(new BigInteger(54_123_711_340_206_195), deployment.CreatorBuyQuote);
		}
		finally
		{
			await host.StopAsync(cancellationToken);
		}
	}

	[Fact]
	public async Task NonLaunchCalldataLeavesCalldataFieldsEmpty()
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
			launchpads: new Dictionary<string, string> { [factoryReceipt.ContractAddress] = "pons", });
		await host.StartAsync(cancellationToken);

		try
		{
			// A call with unrelated calldata (wrong selector) must not break detection or invent data.
			TransactionReceipt callReceipt = await web3.Eth.TransactionManager.SendTransactionAndWaitForReceiptAsync(
				new TransactionInput
				{
					From = deployer.Address,
					To = factoryReceipt.ContractAddress,
					Data = "0xdeadbeef",
					Gas = new HexBigInteger(500_000),
				},
				cancellationToken);

			string childAddress = callReceipt.Logs[0].Address;

			TokenDeployment deployment = await WaitForDeploymentAsync(
				connectionString,
				childAddress,
				cancellationToken);

			Assert.Equal("pons", deployment.LaunchpadName);
			Assert.Null(deployment.PairTokenAddress);
			Assert.Null(deployment.CreatorBuyQuote);

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

	private static PonsTokenParams CreateTokenParams(string creator) =>
		new()
		{
			Name = "Reddit Founder Cat",
			Symbol = "KARMA",
			Logo = "ipfs://logo",
			Description = "Created with Beast",
			Socials = new PonsSocials(),
			CreatorFeeRecipient = creator,
			CreatorTaxBps = 100,
			BuybackEnabled = true,
			ExpectedEconomics = new byte[32],
			Salt = new byte[32],
		};

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
using Argus.Data;
using Argus.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.StandardTokenEIP20;
using Nethereum.StandardTokenEIP20.ContractDefinition;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace Argus.Worker.IntegrationTests;

public sealed class TokenDeploymentDetectionTests(PostgresFixture postgres, AnvilFixture anvil)
{
	/// <summary>
	/// Hand-assembled creation bytecode of a minimal factory: any call to the deployed contract
	/// executes CREATE on a 10-byte child init code that emits LOG0 during construction and
	/// returns a 1-byte (STOP) runtime. Mirrors the launchpad pattern where the token is created
	/// by an internal CREATE, invisible to receipt.contractAddress.
	/// </summary>
	private const string FactoryInitCode =
		"0x756960006000a060016000f3600052600a60166000f000" + "6000526016600af3";

	[Fact]
	public async Task DeployingErc20WritesTokenDeploymentRow()
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		string connectionString = await this.CreateMigratedDatabaseAsync(cancellationToken);

		using IHost host = this.BuildListenerHost(connectionString);
		await host.StartAsync(cancellationToken);

		try
		{
			Account deployer = new(AnvilFixture.DeployerPrivateKey, AnvilFixture.ChainId);
			Web3 web3 = new(deployer, anvil.RpcUrl);

			TransactionReceipt receipt = await StandardTokenService.DeployContractAndWaitForReceiptAsync(
				web3,
				new EIP20Deployment { InitialAmount = 1_000_000, TokenName = "Focus Token", TokenSymbol = "FOCUS", DecimalUnits = 18, });

			TokenDeployment deployment = await WaitForDeploymentAsync(connectionString, receipt.ContractAddress, cancellationToken);

			Assert.Equal(AnvilFixture.ChainId, deployment.ChainId);
			Assert.Equal((long)receipt.BlockNumber.Value, deployment.BlockNumber);
			Assert.Equal(receipt.TransactionHash.ToLowerInvariant(), deployment.TransactionHash);
			Assert.Equal(deployer.Address.ToLowerInvariant(), deployment.DeployerAddress);
			Assert.Null(deployment.FactoryAddress);
			Assert.Null(deployment.LaunchpadName);
			Assert.NotEmpty(deployment.BlockHash);
			Assert.Equal("Focus Token", deployment.TokenName);
			Assert.Equal("FOCUS", deployment.TokenSymbol);
			Assert.Equal(18, deployment.TokenDecimals);
		}
		finally
		{
			await host.StopAsync(cancellationToken);
		}
	}

	[Fact]
	public async Task FactoryCreatedContractIsDetectedWithFactoryAddressAndLaunchpadName()
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		string connectionString = await this.CreateMigratedDatabaseAsync(cancellationToken);

		Account deployer = new(AnvilFixture.DeployerPrivateKey, AnvilFixture.ChainId);
		Web3 web3 = new(deployer, anvil.RpcUrl);

		// The factory must exist before the listener starts so it can be configured as a launchpad.
		TransactionReceipt factoryReceipt = await web3.Eth.TransactionManager.SendTransactionAndWaitForReceiptAsync(
			new TransactionInput { From = deployer.Address, Data = FactoryInitCode, Gas = new HexBigInteger(500_000), },
			cancellationToken);

		using IHost host = this.BuildListenerHost(
			connectionString,
			launchpads: new Dictionary<string, string> { [factoryReceipt.ContractAddress] = "testpad", });
		await host.StartAsync(cancellationToken);

		try
		{
			TransactionReceipt callReceipt = await web3.Eth.TransactionManager.SendTransactionAndWaitForReceiptAsync(
				new TransactionInput { From = deployer.Address, To = factoryReceipt.ContractAddress, Gas = new HexBigInteger(500_000), },
				cancellationToken);

			// The child's construction LOG0 is the only log of the call - its address is the created contract.
			string childAddress = callReceipt.Logs[0].Address;

			TokenDeployment deployment = await WaitForDeploymentAsync(connectionString, childAddress, cancellationToken);

			Assert.Equal(AnvilFixture.ChainId, deployment.ChainId);
			Assert.Equal((long)callReceipt.BlockNumber.Value, deployment.BlockNumber);
			Assert.Equal(callReceipt.TransactionHash.ToLowerInvariant(), deployment.TransactionHash);
			Assert.Equal(deployer.Address.ToLowerInvariant(), deployment.DeployerAddress);
			Assert.Equal(factoryReceipt.ContractAddress.ToLowerInvariant(), deployment.FactoryAddress);
			Assert.Equal("testpad", deployment.LaunchpadName);
			Assert.Null(deployment.TokenName);
			Assert.Null(deployment.TokenSymbol);
		}
		finally
		{
			await host.StopAsync(cancellationToken);
		}
	}

	[Fact]
	public async Task LogsModeDetectsFactoryCreatedContract()
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		string connectionString = await this.CreateMigratedDatabaseAsync(cancellationToken);

		using IHost host = this.BuildListenerHost(connectionString, IngestMode.Logs);
		await host.StartAsync(cancellationToken);

		try
		{
			Account deployer = new(AnvilFixture.DeployerPrivateKey, AnvilFixture.ChainId);
			Web3 web3 = new(deployer, anvil.RpcUrl);

			TransactionReceipt factoryReceipt = await web3.Eth.TransactionManager.SendTransactionAndWaitForReceiptAsync(
				new TransactionInput { From = deployer.Address, Data = FactoryInitCode, Gas = new HexBigInteger(500_000), },
				cancellationToken);

			TransactionReceipt callReceipt = await web3.Eth.TransactionManager.SendTransactionAndWaitForReceiptAsync(
				new TransactionInput { From = deployer.Address, To = factoryReceipt.ContractAddress, Gas = new HexBigInteger(500_000), },
				cancellationToken);

			string childAddress = callReceipt.Logs[0].Address;

			TokenDeployment deployment = await WaitForDeploymentAsync(connectionString, childAddress, cancellationToken);

			Assert.Equal(callReceipt.TransactionHash.ToLowerInvariant(), deployment.TransactionHash);
			Assert.Equal(deployer.Address.ToLowerInvariant(), deployment.DeployerAddress);
			Assert.Equal(factoryReceipt.ContractAddress.ToLowerInvariant(), deployment.FactoryAddress);
		}
		finally
		{
			await host.StopAsync(cancellationToken);
		}
	}

	[Fact]
	public async Task ListenerResumesFromPersistedCursorAndClosesTheGap()
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		string connectionString = await this.CreateMigratedDatabaseAsync(cancellationToken);

		Account deployer = new(AnvilFixture.DeployerPrivateKey, AnvilFixture.ChainId);
		Web3 web3 = new(deployer, anvil.RpcUrl);

		// A previous listener run stopped at the current tip.
		long stoppedAt = (long)(await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value;
		await using (AppDbContext dbContext = CreateDbContext(connectionString))
		{
			dbContext.ChainCursors.Add(
				new ChainCursor { ChainId = AnvilFixture.ChainId, LastProcessedBlock = stoppedAt, UpdatedAt = DateTimeOffset.UtcNow, });
			await dbContext.SaveChangesAsync(cancellationToken);
		}

		// While the listener is down, a token launches - and more blocks are mined on top,
		// so a fresh tip-start would never see the deployment block.
		TransactionReceipt receipt = await StandardTokenService.DeployContractAndWaitForReceiptAsync(
			web3,
			new EIP20Deployment { InitialAmount = 1_000_000, TokenName = "Gap Token", TokenSymbol = "GAP", DecimalUnits = 18, });

		for (int i = 0; i < 3; i++)
		{
			await web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(deployer.Address, 0.001m, cancellationToken: TestContext.Current.CancellationToken);
		}

		using IHost host = this.BuildListenerHost(connectionString);
		await host.StartAsync(cancellationToken);

		try
		{
			TokenDeployment deployment = await WaitForDeploymentAsync(connectionString, receipt.ContractAddress, cancellationToken);

			Assert.Equal((long)receipt.BlockNumber.Value, deployment.BlockNumber);

			await using AppDbContext dbContext = CreateDbContext(connectionString);
			ChainCursor cursor = await dbContext.ChainCursors.SingleAsync(
				entity => entity.ChainId == AnvilFixture.ChainId,
				cancellationToken);
			Assert.True(
				cursor.LastProcessedBlock >= deployment.BlockNumber,
				$"Cursor should have advanced to at least {deployment.BlockNumber}, but is at {cursor.LastProcessedBlock}.");
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

	private IHost BuildListenerHost(
		string connectionString,
		IngestMode mode = IngestMode.BlockReceipts,
		Dictionary<string, string>? launchpads = null)
	{
		HostApplicationBuilder builder = Host.CreateApplicationBuilder();
		builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
		builder.Services.Configure<ChainIngestOptions>(options =>
		{
			options.RpcUrls = anvil.RpcUrl;
			options.Confirmations = 0;
			options.PollInterval = TimeSpan.FromMilliseconds(250);
			options.Mode = mode;
			options.Launchpads = launchpads ?? [];
		});
		builder.Services.AddHostedService<EvmDeploymentListener>();

		return builder.Build();
	}
}
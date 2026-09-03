using System.Net;
using System.Numerics;
using Argus.Data;
using Argus.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace Argus.Worker;

/// <summary>
/// Polls an EVM chain for contract deployments and records them as <see cref="TokenDeployment" /> rows.
/// Blocks are only processed once they are <see cref="ChainIngestOptions.Confirmations" /> blocks behind
/// the tip; the same sequential walk doubles as catch-up after downtime.
/// Fast chains (Orbit chains run at ~10 blocks/s) dictate the shape: one eth_getBlockReceipts per block
/// (fetched in parallel) yields plain creations, logs, and attribution in a single call, and a
/// known-contract cache keeps eth_getCode checks to genuinely new log emitters.
/// </summary>
public sealed partial class EvmDeploymentListener(
	IOptions<ChainIngestOptions> options,
	IServiceScopeFactory scopeFactory,
	ILogger<EvmDeploymentListener> logger) : BackgroundService
{
	private const int MaxParallelRpcCalls = 8;

	private const int KnownContractCacheLimit = 200_000;

	private const string NativeQuoteAddress = "0x0000000000000000000000000000000000000000";

	private readonly HashSet<string> knownContracts = [];

	private Dictionary<string, string> launchpadsByAddress = [];

	private long? lastProcessedBlock;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		ChainIngestOptions ingest = options.Value;
		string[] endpoints = ingest.RpcUrls.Split(
			';',
			StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		if (endpoints.Length == 0)
		{
			throw new InvalidOperationException("Intel:Ingest:RpcUrls must contain at least one RPC endpoint.");
		}

		Web3[] clients = [.. endpoints.Select(endpoint => new Web3(endpoint))];
		int active = 0;

		this.launchpadsByAddress = ingest.Launchpads.ToDictionary(
			pair => pair.Key.ToLowerInvariant(),
			pair => pair.Value);

		long? chainId = null;
		while (chainId is null && !stoppingToken.IsCancellationRequested)
		{
			try
			{
				chainId = (long)(await clients[active].Eth.ChainId.SendRequestAsync()).Value;
			}
			catch (Exception exception) when (exception is not OperationCanceledException)
			{
				LogPollingFailed(logger, exception, 0);
				active = (active + 1) % clients.Length;
				await Task.Delay(ingest.PollInterval, stoppingToken);
			}
		}

		if (chainId is not long resolvedChainId)
		{
			return;
		}

		this.lastProcessedBlock = await this.LoadCursorAsync(resolvedChainId, stoppingToken);

		LogListening(logger, resolvedChainId, endpoints[active]);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await this.ProcessNewBlocksAsync(clients[active], resolvedChainId, ingest, stoppingToken);
			}
			catch (Exception exception) when (exception is not OperationCanceledException)
			{
				LogPollingFailed(logger, exception, resolvedChainId);

				if (clients.Length > 1)
				{
					active = (active + 1) % clients.Length;
					LogEndpointSwitched(logger, resolvedChainId, endpoints[active]);
				}
			}

			await Task.Delay(ingest.PollInterval, stoppingToken);
		}
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Listening for deployments on chain {ChainId} via {RpcUrl}")]
	private static partial void LogListening(ILogger logger, long chainId, string rpcUrl);

	[LoggerMessage(
		Level = LogLevel.Error,
		Message = "Polling chain {ChainId} failed; retrying after the poll interval")]
	private static partial void LogPollingFailed(ILogger logger, Exception exception, long chainId);

	[LoggerMessage(
		Level = LogLevel.Warning,
		Message =
			"Catch-up gap on chain {ChainId} exceeds MaxCatchUpBlocks - skipping blocks {FromBlock} through {ToBlock}")]
	private static partial void LogGapSkipped(ILogger logger, long chainId, long fromBlock, long toBlock);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Switching chain {ChainId} ingestion to RPC endpoint {RpcUrl}")]
	private static partial void LogEndpointSwitched(ILogger logger, long chainId, string rpcUrl);

	[LoggerMessage(
		Level = LogLevel.Information,
		Message =
			"Detected deployment {ContractAddress} (token: {TokenSymbol}, launchpad: {LaunchpadName}) by {DeployerAddress} on chain {ChainId} in block {BlockNumber}")]
	private static partial void LogDeploymentDetected(
		ILogger logger,
		string contractAddress,
		string? tokenSymbol,
		string? launchpadName,
		string deployerAddress,
		long chainId,
		long blockNumber);

	private static async Task<TResult?> QueryOrDefaultAsync<TFunction, TResult>(
		Web3 web3,
		string contractAddress,
		CancellationToken cancellationToken)
		where TFunction : FunctionMessage, new()
	{
		try
		{
			return await ExecuteWithRetryAsync(
				() => web3.Eth.GetContractQueryHandler<TFunction>()
					.QueryAsync<TResult>(contractAddress, new TFunction()),
				cancellationToken);
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			// Not a token (revert, no code, undecodable return data) - metadata stays null.
			return default;
		}
	}

	private static async Task<(string? Name, string? Symbol, int? Decimals)> ReadErc20MetadataAsync(
		Web3 web3,
		string contractAddress,
		CancellationToken cancellationToken)
	{
		string? name = await QueryOrDefaultAsync<NameFunction, string>(web3, contractAddress, cancellationToken);
		string? symbol = await QueryOrDefaultAsync<SymbolFunction, string>(web3, contractAddress, cancellationToken);

		if (name is null && symbol is null)
		{
			return (null, null, null);
		}

		int? decimals = await QueryOrDefaultAsync<DecimalsFunction, byte>(web3, contractAddress, cancellationToken);

		return (name, symbol, decimals);
	}

	/// <summary>
	/// Decodes the Pons launch calldata of the deployment transaction - launchAndBuy (creator buys at
	/// launch) or launchToken (no buy) - into the insider whitelist (snipeTaxExemptions), the
	/// creator's own launch buy, and the quote asset. Factory transactions with other calldata leave
	/// the launch fields empty.
	/// </summary>
	private static async Task DecodePonsLaunchAsync(
		Web3 web3,
		TokenDeployment deployment,
		CancellationToken cancellationToken)
	{
		try
		{
			Transaction transaction = await ExecuteWithRetryAsync(
				() => web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(deployment.TransactionHash),
				cancellationToken);

			if (transaction is null)
			{
				return;
			}

			if (transaction.IsTransactionForFunctionMessage<PonsLaunchAndBuyFunction>())
			{
				PonsLaunchAndBuyFunction launch =
					transaction.DecodeTransactionToFunctionMessage<PonsLaunchAndBuyFunction>();
				ApplyPonsLaunch(deployment, launch.PairToken, launch.QuoteIn, launch.SnipeTaxExemptions);
			}
			else if (transaction.IsTransactionForFunctionMessage<PonsLaunchTokenFunction>())
			{
				PonsLaunchTokenFunction launch =
					transaction.DecodeTransactionToFunctionMessage<PonsLaunchTokenFunction>();
				ApplyPonsLaunch(deployment, launch.PairToken, null, launch.SnipeTaxExemptions);
			}
			else if (transaction.IsTransactionForFunctionMessage<PonsLaunchTokenWithoutExemptionsFunction>())
			{
				PonsLaunchTokenWithoutExemptionsFunction launch =
					transaction.DecodeTransactionToFunctionMessage<PonsLaunchTokenWithoutExemptionsFunction>();
				ApplyPonsLaunch(deployment, launch.PairToken, null, []);
			}
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			// Malformed or unexpected calldata - detection still stands, only the launch details stay empty.
		}
	}

	private static void ApplyPonsLaunch(
		TokenDeployment deployment,
		string pairToken,
		BigInteger? creatorBuy,
		IEnumerable<string> insiders)
	{
		deployment.PairTokenAddress = pairToken.ToLowerInvariant();
		deployment.CreatorBuyQuote = creatorBuy;
		deployment.Insiders =
		[
			.. insiders.Select(address => address.ToLowerInvariant())
				.Distinct()
				.Select(address => new TokenDeploymentInsider { Address = address, }),
		];
	}

	/// <summary>
	/// Retries an RPC call when the endpoint rate-limits (HTTP 429). The public Robinhood Chain
	/// endpoint uses a 60-second window, so the last delay outlasts a full window.
	/// </summary>
	private static async Task<T> ExecuteWithRetryAsync<T>(
		Func<Task<T>> sendRequest,
		CancellationToken cancellationToken)
	{
		for (int attempt = 1; ; attempt++)
		{
			try
			{
				return await sendRequest();
			}
			catch (RpcClientUnknownException exception) when (attempt <= 3 && IsRateLimited(exception))
			{
				TimeSpan delay = attempt switch
				{
					1 => TimeSpan.FromSeconds(5),
					2 => TimeSpan.FromSeconds(20),
					_ => TimeSpan.FromSeconds(65),
				};

				await Task.Delay(delay, cancellationToken);
			}
		}
	}

	private static bool IsRateLimited(RpcClientUnknownException exception) =>
		exception.InnerException is HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests };

	/// <summary>
	/// Decodes a Uniswap Liquidity Launchpad (pools.trade) launch: the distribution strategy and the
	/// creator's optional native first buy. These launches are quoted in native currency, so the
	/// quote asset is recorded as the zero address.
	/// </summary>
	private static async Task DecodeUniswapLaunchAsync(
		Web3 web3,
		TokenDeployment deployment,
		CancellationToken cancellationToken)
	{
		try
		{
			Transaction transaction = await ExecuteWithRetryAsync(
				() => web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(deployment.TransactionHash),
				cancellationToken);

			if (UniswapLaunchDecoder.TryDecode(transaction?.Input) is not { } launch)
			{
				return;
			}

			deployment.PairTokenAddress = NativeQuoteAddress;
			deployment.CreatorBuyQuote = launch.CreatorBuyNative;
			deployment.LaunchStrategyAddress = launch.StrategyAddress;
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			// Malformed or unexpected calldata - detection still stands, only the launch details stay empty.
		}
	}

	private static TokenDeployment ToDeployment(long chainId, TransactionReceipt receipt, string contractAddress) =>
		new()
		{
			ChainId = chainId,
			BlockNumber = (long)receipt.BlockNumber.Value,
			BlockHash = receipt.BlockHash.ToLowerInvariant(),
			TransactionHash = receipt.TransactionHash.ToLowerInvariant(),
			ContractAddress = contractAddress.ToLowerInvariant(),
			DeployerAddress = receipt.From.ToLowerInvariant(),
			FactoryAddress = receipt.To?.ToLowerInvariant(),
			DetectedAt = DateTimeOffset.UtcNow,
		};

	private static async Task<TransactionReceipt[]> FetchBlockReceiptsAsync(
		Web3 web3,
		long blockNumber,
		SemaphoreSlim throttle,
		CancellationToken cancellationToken)
	{
		await throttle.WaitAsync(cancellationToken);

		try
		{
			return await ExecuteWithRetryAsync(
				() => web3.Eth.Blocks.GetBlockReceiptsByNumber.SendRequestAsync(new HexBigInteger(blockNumber)),
				cancellationToken);
		}
		finally
		{
			throttle.Release();
		}
	}

	private static async Task<bool> IsNewContractAsync(
		Web3 web3,
		string address,
		long blockNumber,
		SemaphoreSlim throttle,
		CancellationToken cancellationToken)
	{
		await throttle.WaitAsync(cancellationToken);

		try
		{
			string code = await ExecuteWithRetryAsync(
				() => web3.Eth.GetCode.SendRequestAsync(
					address,
					new BlockParameter(new HexBigInteger(blockNumber - 1))),
				cancellationToken);

			return code is null or "" or "0x";
		}
		finally
		{
			throttle.Release();
		}
	}

	private async Task ProcessNewBlocksAsync(
		Web3 web3,
		long chainId,
		ChainIngestOptions ingest,
		CancellationToken cancellationToken)
	{
		long latest = (long)(await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value;
		long confirmedTip = latest - ingest.Confirmations;

		if (confirmedTip < 0)
		{
			return;
		}

		long next = this.lastProcessedBlock is long processed ? processed + 1 : confirmedTip;

		long minimumStart = confirmedTip - ingest.MaxCatchUpBlocks + 1;
		if (next < minimumStart)
		{
			LogGapSkipped(logger, chainId, next, minimumStart - 1);
			next = minimumStart;
		}

		while (next <= confirmedTip)
		{
			cancellationToken.ThrowIfCancellationRequested();

			long chunkEnd = Math.Min(next + ingest.ChunkBlocks - 1, confirmedTip);

			if (ingest.Mode == IngestMode.Logs)
			{
				await this.ProcessBlockRangeViaLogsAsync(web3, chainId, next, chunkEnd, cancellationToken);
			}
			else
			{
				await this.ProcessBlockRangeAsync(web3, chainId, next, chunkEnd, cancellationToken);
			}

			this.lastProcessedBlock = chunkEnd;
			await this.PersistCursorAsync(chainId, chunkEnd, cancellationToken);

			next = chunkEnd + 1;
		}
	}

	/// <summary>
	/// Budget mode for rate-limited public RPC endpoints: one eth_getLogs for the whole range,
	/// then per-new-address lookups only. Contracts that never emit a log in their creation
	/// transaction are deliberately not detected in this mode.
	/// </summary>
	private async Task ProcessBlockRangeViaLogsAsync(
		Web3 web3,
		long chainId,
		long fromBlock,
		long toBlock,
		CancellationToken cancellationToken)
	{
		NewFilterInput filter = new()
		{
			FromBlock = new BlockParameter(new HexBigInteger(fromBlock)),
			ToBlock = new BlockParameter(new HexBigInteger(toBlock)),
		};
		FilterLog[] logs = await ExecuteWithRetryAsync(
			() => web3.Eth.Filters.GetLogs.SendRequestAsync(filter),
			cancellationToken);

		List<FilterLog> candidates = [];
		HashSet<string> candidateAddresses = [];

		// Logs are in execution order, so the first log per address belongs to the earliest transaction.
		foreach (FilterLog log in logs)
		{
			if ((long)log.BlockNumber.Value == 0)
			{
				continue;
			}

			string address = log.Address.ToLowerInvariant();

			if (this.knownContracts.Contains(address) || !candidateAddresses.Add(address))
			{
				continue;
			}

			candidates.Add(log);
		}

		using SemaphoreSlim throttle = new(MaxParallelRpcCalls);
		bool[] isNew = await Task.WhenAll(
			candidates.Select(log => IsNewContractAsync(
				web3,
				log.Address,
				(long)log.BlockNumber.Value,
				throttle,
				cancellationToken)));

		for (int i = 0; i < candidates.Count; i++)
		{
			FilterLog log = candidates[i];
			this.RememberContract(log.Address);

			if (!isNew[i])
			{
				continue;
			}

			// The first log of a fresh contract is virtually always in its creation transaction
			// (launchpads mint at launch); its receipt carries deployer/factory attribution.
			TransactionReceipt receipt = await ExecuteWithRetryAsync(
				() => web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(log.TransactionHash),
				cancellationToken);

			await this.StoreDeploymentAsync(web3, ToDeployment(chainId, receipt, log.Address), cancellationToken);
		}
	}

	private async Task ProcessBlockRangeAsync(
		Web3 web3,
		long chainId,
		long fromBlock,
		long toBlock,
		CancellationToken cancellationToken)
	{
		using SemaphoreSlim throttle = new(MaxParallelRpcCalls);

		Task<TransactionReceipt[]>[] receiptFetches =
		[
			.. Enumerable.Range(0, (int)(toBlock - fromBlock + 1))
				.Select(offset => FetchBlockReceiptsAsync(web3, fromBlock + offset, throttle, cancellationToken)),
		];
		TransactionReceipt[][] receiptsPerBlock = await Task.WhenAll(receiptFetches);

		List<TokenDeployment> deployments = [];
		List<(string Address, TransactionReceipt Receipt)> candidates = [];
		HashSet<string> candidateAddresses = [];

		foreach (TransactionReceipt receipt in receiptsPerBlock.SelectMany(receipts => receipts))
		{
			if (receipt.Status is { } status && status.Value == 0)
			{
				continue;
			}

			// A top-level CREATE surfaces directly on the receipt.
			if (receipt.ContractAddress is { } contractAddress)
			{
				this.RememberContract(contractAddress);
				deployments.Add(ToDeployment(chainId, receipt, contractAddress));
			}

			if ((long)receipt.BlockNumber.Value == 0)
			{
				continue;
			}

			// A contract created by an internal CREATE (launchpad/factory pattern) never surfaces via
			// receipt.contractAddress - but any log emitter without code in the previous block was
			// deployed in this one.
			foreach (FilterLog log in receipt.Logs)
			{
				string address = log.Address.ToLowerInvariant();

				if (this.knownContracts.Contains(address) || !candidateAddresses.Add(address))
				{
					continue;
				}

				candidates.Add((log.Address, receipt));
			}
		}

		bool[] isNew = await Task.WhenAll(
			candidates.Select(candidate => IsNewContractAsync(
				web3,
				candidate.Address,
				(long)candidate.Receipt.BlockNumber.Value,
				throttle,
				cancellationToken)));

		for (int i = 0; i < candidates.Count; i++)
		{
			this.RememberContract(candidates[i].Address);

			if (isNew[i])
			{
				deployments.Add(ToDeployment(chainId, candidates[i].Receipt, candidates[i].Address));
			}
		}

		foreach (TokenDeployment deployment in deployments)
		{
			await this.StoreDeploymentAsync(web3, deployment, cancellationToken);
		}
	}

	private void RememberContract(string address)
	{
		if (this.knownContracts.Count >= KnownContractCacheLimit)
		{
			this.knownContracts.Clear();
		}

		this.knownContracts.Add(address.ToLowerInvariant());
	}

	private async Task<long?> LoadCursorAsync(long chainId, CancellationToken cancellationToken)
	{
		using IServiceScope scope = scopeFactory.CreateScope();
		AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		ChainCursor? cursor = await dbContext.ChainCursors.FindAsync([chainId,], cancellationToken);

		return cursor?.LastProcessedBlock;
	}

	private async Task PersistCursorAsync(long chainId, long blockNumber, CancellationToken cancellationToken)
	{
		using IServiceScope scope = scopeFactory.CreateScope();
		AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		ChainCursor? cursor = await dbContext.ChainCursors.FindAsync([chainId,], cancellationToken);

		if (cursor is null)
		{
			dbContext.ChainCursors.Add(
				new ChainCursor { ChainId = chainId, LastProcessedBlock = blockNumber, UpdatedAt = DateTimeOffset.UtcNow, });
		}
		else
		{
			cursor.LastProcessedBlock = blockNumber;
			cursor.UpdatedAt = DateTimeOffset.UtcNow;
		}

		await dbContext.SaveChangesAsync(cancellationToken);
	}

	private async Task StoreDeploymentAsync(Web3 web3, TokenDeployment deployment, CancellationToken cancellationToken)
	{
		using IServiceScope scope = scopeFactory.CreateScope();
		AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		bool exists = await dbContext.TokenDeployments.AnyAsync(
			entity => entity.ChainId == deployment.ChainId && entity.ContractAddress == deployment.ContractAddress,
			cancellationToken);

		if (exists)
		{
			return;
		}

		if (deployment.FactoryAddress is { } factory &&
			this.launchpadsByAddress.TryGetValue(factory, out string? launchpadName))
		{
			deployment.LaunchpadName = launchpadName;

			switch (launchpadName)
			{
				case "pons":
					await DecodePonsLaunchAsync(web3, deployment, cancellationToken);
					break;
				case "uniswap":
					await DecodeUniswapLaunchAsync(web3, deployment, cancellationToken);
					break;
			}
		}

		(deployment.TokenName, deployment.TokenSymbol, deployment.TokenDecimals) =
			await ReadErc20MetadataAsync(web3, deployment.ContractAddress, cancellationToken);

		dbContext.TokenDeployments.Add(deployment);
		await dbContext.SaveChangesAsync(cancellationToken);

		LogDeploymentDetected(
			logger,
			deployment.ContractAddress,
			deployment.TokenSymbol,
			deployment.LaunchpadName,
			deployment.DeployerAddress,
			deployment.ChainId,
			deployment.BlockNumber);
	}
}
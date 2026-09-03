namespace FocusTemplate.Intel.Worker;

/// <summary>
/// Configuration for a single EVM chain listener.
/// </summary>
public sealed class ChainIngestOptions
{
	public const string SectionName = "Intel:Ingest";

	/// <summary>
	/// Gets or sets the RPC endpoints in priority order, separated by semicolons. The first entry
	/// is the primary; on repeated failures the listener rotates to the next one.
	/// </summary>
	public string RpcUrls { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets number of blocks behind the tip a block must be before it is processed (reorg safety).
	/// </summary>
	public int Confirmations { get; set; }

	public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

	/// <summary>
	/// Gets or sets the maximum number of blocks walked in one catch-up. A larger gap is skipped
	/// with a warning to avoid an unbounded backfill against a fast chain.
	/// </summary>
	public int MaxCatchUpBlocks { get; set; } = 1000;

	public IngestMode Mode { get; set; } = IngestMode.BlockReceipts;

	/// <summary>
	/// Gets or sets the number of blocks processed per chunk (one eth_getLogs range in Logs mode).
	/// Providers cap getLogs responses - on a busy chain a smaller chunk keeps ranges under the cap
	/// (Alchemy rejects ~100-block ranges on Robinhood Chain with 400).
	/// </summary>
	public int ChunkBlocks { get; set; } = 100;

	/// <summary>
	/// Gets or sets known launchpad factories: contract address to launchpad name
	/// (e.g. "0xe33e..." to "pons"). Deployments through these get their LaunchpadName set.
	/// </summary>
	public Dictionary<string, string> Launchpads { get; set; } = [];
}
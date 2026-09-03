namespace Argus.Worker;

/// <summary>
/// How the listener discovers deployments in a block range.
/// </summary>
public enum IngestMode
{
	/// <summary>
	/// One eth_getBlockReceipts per block: complete coverage (including log-less top-level
	/// CREATEs), but too many requests for rate-limited public RPC endpoints on fast chains.
	/// </summary>
	BlockReceipts,

	/// <summary>
	/// One eth_getLogs per range plus lookups for new addresses only: fits free-tier rate
	/// limits on ~10 blocks/s chains. Catches everything that emits a log at creation (every
	/// token/launchpad launch); deliberately misses contracts that never log in their creation
	/// transaction.
	/// </summary>
	Logs,
}
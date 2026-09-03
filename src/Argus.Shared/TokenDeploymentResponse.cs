namespace Argus.Shared;

/// <summary>
/// A detected contract deployment, plus the launch signals decoded from known launchpad calldata
/// and the deterministic T+0 assessment derived from them.
/// <para>
/// PairTokenAddress is the quote asset: the zero address means natively quoted (canonical), null
/// means no launch calldata was decoded. CreatorBuyQuote is the creator's own launch buy in the
/// quote asset's smallest unit, carried as a decimal string because a uint256 exceeds every JSON
/// number type. InsiderCount is the number of addresses the creator exempted from the launch
/// snipe tax (Pons snipeTaxExemptions) - any non-zero count is the extractor pattern. Flags are
/// the fired detectors and Verdict their combined outcome under RulesVersion.
/// </para>
/// </summary>
public sealed record TokenDeploymentResponse(
	long ChainId,
	string ContractAddress,
	string DeployerAddress,
	string? FactoryAddress,
	string? LaunchpadName,
	string? TokenName,
	string? TokenSymbol,
	int? TokenDecimals,
	string? PairTokenAddress,
	string? CreatorBuyQuote,
	int InsiderCount,
	string TransactionHash,
	long BlockNumber,
	DateTimeOffset DetectedAt,
	LaunchVerdict Verdict,
	string RulesVersion,
	IReadOnlyList<LaunchFlagResponse> Flags);
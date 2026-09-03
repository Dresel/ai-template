using System.Numerics;

namespace Argus.Api;

/// <summary>
/// The stored facts about one launch that the T+0 rules evaluate.
/// </summary>
public sealed record LaunchFacts(
	string? LaunchpadName,
	int InsiderCount,
	string? PairTokenAddress,
	BigInteger? CreatorBuyQuote,
	int OtherLaunchesByDeployer,
	string? LaunchStrategyAddress = null);
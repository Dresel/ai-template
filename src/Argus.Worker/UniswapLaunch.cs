using System.Numerics;

namespace Argus.Worker;

/// <summary>
/// What a Uniswap Liquidity Launchpad launch transaction reveals: the token metadata from
/// createToken, the distribution strategy, and the creator's optional first buy in native currency.
/// </summary>
public sealed record UniswapLaunch(
	string? TokenName,
	string? TokenSymbol,
	string? StrategyAddress,
	BigInteger? CreatorBuyNative);
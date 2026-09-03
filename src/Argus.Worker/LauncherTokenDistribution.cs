using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Argus.Worker;

/// <summary>
/// The distribution tuple of LiquidityLauncher.distributeToken: which strategy contract (instant
/// launch, crowd auction, LBP) receives how much supply, plus its strategy-specific config.
/// </summary>
public sealed class LauncherTokenDistribution
{
	[Parameter("address", "strategy", 1)]
	public string Strategy { get; set; } = string.Empty;

	[Parameter("uint128", "amount", 2)]
	public BigInteger Amount { get; set; }

	[Parameter("bytes", "configData", 3)]
	public byte[] ConfigData { get; set; } = [];
}
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Argus.Worker;

/// <summary>
/// Uniswap Liquidity Launchpad launches are one LiquidityLauncher.multicall carrying the inner
/// createToken / distributeToken / distributeWithNative calls.
/// </summary>
[Function("multicall")]
public sealed class LiquidityLauncherMulticallFunction : FunctionMessage
{
	[Parameter("bytes[]", "data", 1)]
	public List<byte[]> Data { get; set; } = [];
}
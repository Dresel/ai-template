using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Argus.Worker;

/// <summary>
/// LiquidityLauncher.distributeWithNative: the creator's own first buy, routed through the
/// UniversalRouter strategy with the attached native amount - the Pons quoteIn equivalent.
/// </summary>
[Function("distributeWithNative")]
public sealed class LauncherDistributeWithNativeFunction : FunctionMessage
{
	[Parameter("address", "strategy", 1)]
	public string Strategy { get; set; } = string.Empty;

	[Parameter("bytes", "configData", 2)]
	public byte[] ConfigData { get; set; } = [];

	[Parameter("bytes32", "salt", 3)]
	public byte[] Salt { get; set; } = [];

	[Parameter("uint256", "nativeAmount", 4)]
	public BigInteger NativeAmount { get; set; }
}
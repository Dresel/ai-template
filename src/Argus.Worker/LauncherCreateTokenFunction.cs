using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Argus.Worker;

/// <summary>
/// LiquidityLauncher.createToken: mints the fixed-supply UERC20 through the token factory. The
/// tokenData blob carries description, image, and the creator's X-verification metadata.
/// </summary>
[Function("createToken")]
public sealed class LauncherCreateTokenFunction : FunctionMessage
{
	[Parameter("address", "factory", 1)]
	public string Factory { get; set; } = string.Empty;

	[Parameter("string", "name", 2)]
	public string Name { get; set; } = string.Empty;

	[Parameter("string", "symbol", 3)]
	public string Symbol { get; set; } = string.Empty;

	[Parameter("uint8", "decimals", 4)]
	public byte Decimals { get; set; }

	[Parameter("uint128", "initialSupply", 5)]
	public BigInteger InitialSupply { get; set; }

	[Parameter("address", "recipient", 6)]
	public string Recipient { get; set; } = string.Empty;

	[Parameter("bytes", "tokenData", 7)]
	public byte[] TokenData { get; set; } = [];
}
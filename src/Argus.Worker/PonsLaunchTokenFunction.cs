using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Argus.Worker;

/// <summary>
/// PonsV2LaunchFactory.launchToken (the launch-without-buy entry point, factory
/// 0x7ed598bcef8bd9edd8c97a195c6d13f40801ec7e on Robinhood Chain) - the insider whitelist travels
/// here just like in launchAndBuy, only the creator buy is absent.
/// </summary>
[Function("launchToken")]
public sealed class PonsLaunchTokenFunction : FunctionMessage
{
	[Parameter("tuple", "params", 1)]
	public PonsTokenParams Params { get; set; } = new();

	[Parameter("uint256", "launchConfigId", 2)]
	public BigInteger LaunchConfigId { get; set; }

	[Parameter("address", "pairToken", 3)]
	public string PairToken { get; set; } = string.Empty;

	[Parameter("address[]", "snipeTaxExemptions", 4)]
	public List<string> SnipeTaxExemptions { get; set; } = [];
}
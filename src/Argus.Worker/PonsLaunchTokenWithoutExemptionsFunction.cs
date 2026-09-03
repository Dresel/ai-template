using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Argus.Worker;

/// <summary>
/// The PonsV2LaunchFactory.launchToken overload without a whitelist - same selector name, different
/// parameter list, so a separate message type.
/// </summary>
[Function("launchToken")]
public sealed class PonsLaunchTokenWithoutExemptionsFunction : FunctionMessage
{
	[Parameter("tuple", "params", 1)]
	public PonsTokenParams Params { get; set; } = new();

	[Parameter("uint256", "launchConfigId", 2)]
	public BigInteger LaunchConfigId { get; set; }

	[Parameter("address", "pairToken", 3)]
	public string PairToken { get; set; } = string.Empty;
}
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Argus.Worker;

[Function("distributeToken")]
public sealed class LauncherDistributeTokenFunction : FunctionMessage
{
	[Parameter("address", "token", 1)]
	public string Token { get; set; } = string.Empty;

	[Parameter("tuple", "distribution", 2)]
	public LauncherTokenDistribution Distribution { get; set; } = new();

	[Parameter("bytes32", "salt", 3)]
	public byte[] Salt { get; set; } = [];
}
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Argus.Worker;

/// <summary>
/// Calldata layout of PonsV2LaunchAndBuy.launchAndBuy, taken from the verified factory
/// 0xe33E9E479dF8802cb0866d5d05258bEc4cF62948 on Robinhood Chain. The snipe-tax exemptions are
/// the creator's insider whitelist and quoteIn is the creator's own launch buy.
/// </summary>
[Function("launchAndBuy")]
public sealed class PonsLaunchAndBuyFunction : FunctionMessage
{
	[Parameter("tuple", "params", 1)]
	public PonsTokenParams Params { get; set; } = new();

	[Parameter("uint256", "launchConfigId", 2)]
	public BigInteger LaunchConfigId { get; set; }

	[Parameter("address", "pairToken", 3)]
	public string PairToken { get; set; } = string.Empty;

	[Parameter("uint256", "quoteIn", 4)]
	public BigInteger QuoteIn { get; set; }

	[Parameter("uint256", "minTokensOut", 5)]
	public BigInteger MinTokensOut { get; set; }

	[Parameter("address", "recipient", 6)]
	public string Recipient { get; set; } = string.Empty;

	[Parameter("address[]", "snipeTaxExemptions", 7)]
	public List<string> SnipeTaxExemptions { get; set; } = [];
}
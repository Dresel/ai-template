using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Argus.Worker;

/// <summary>
/// The PonsV2LaunchFactory.TokenParams tuple inside <see cref="PonsLaunchAndBuyFunction" />.
/// Description and socials feed the metadata heuristics (bot-template launches ship empty links).
/// </summary>
public sealed class PonsTokenParams
{
	[Parameter("string", "name", 1)]
	public string Name { get; set; } = string.Empty;

	[Parameter("string", "symbol", 2)]
	public string Symbol { get; set; } = string.Empty;

	[Parameter("string", "logo", 3)]
	public string Logo { get; set; } = string.Empty;

	[Parameter("string", "description", 4)]
	public string Description { get; set; } = string.Empty;

	[Parameter("tuple", "socials", 5)]
	public PonsSocials Socials { get; set; } = new();

	[Parameter("address", "creatorFeeRecipient", 6)]
	public string CreatorFeeRecipient { get; set; } = string.Empty;

	[Parameter("uint16", "creatorTaxBps", 7)]
	public ushort CreatorTaxBps { get; set; }

	[Parameter("bool", "buybackEnabled", 8)]
	public bool BuybackEnabled { get; set; }

	[Parameter("bytes32", "expectedEconomics", 9)]
	public byte[] ExpectedEconomics { get; set; } = [];

	[Parameter("bytes32", "salt", 10)]
	public byte[] Salt { get; set; } = [];
}
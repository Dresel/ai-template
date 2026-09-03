using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Argus.Worker;

/// <summary>
/// The PonsV2LauncherToken.Socials tuple inside <see cref="PonsTokenParams" />.
/// </summary>
public sealed class PonsSocials
{
	[Parameter("string", "twitter", 1)]
	public string Twitter { get; set; } = string.Empty;

	[Parameter("string", "telegram", 2)]
	public string Telegram { get; set; } = string.Empty;

	[Parameter("string", "discord", 3)]
	public string Discord { get; set; } = string.Empty;

	[Parameter("string", "website", 4)]
	public string Website { get; set; } = string.Empty;

	[Parameter("string", "farcaster", 5)]
	public string Farcaster { get; set; } = string.Empty;
}
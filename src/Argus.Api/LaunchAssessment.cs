using System.Globalization;
using System.Numerics;
using Argus.Shared;

namespace Argus.Api;

/// <summary>
/// Deterministic T+0 launch rules, derived from the 2026-09 field study of Robinhood Chain launches
/// (see shitcoin-trader.md). Pure: stored facts in, verdict and evidence-carrying flags out. Any
/// danger flag rejects, any warning puts the launch on watch, otherwise it is clear - which only
/// means nothing fired at T+0, since creator and insider selling can only be observed afterwards.
/// </summary>
public static class LaunchAssessment
{
	/// <summary>
	/// Identifies the rule set a verdict was produced under, so stored or displayed verdicts stay
	/// auditable when rules change.
	/// </summary>
	public const string RulesVersion = "t0-v1";

	/// <summary>
	/// Other launches by the same deployer at which the serial-launcher flag turns from a warning
	/// into a rejection.
	/// </summary>
	public const int SerialLauncherRejectThreshold = 2;

	private const string NativeQuote = "0x0000000000000000000000000000000000000000";

	private const int NativeDecimals = 18;

	private static readonly (string Label, string Detail) InstantLaunch = ("instant launch",
		"pools.trade Instant Launch: a bonding curve straight into the pool with no minimum - the pump.fun-style format, without an auction or FDV floor.");

	private static readonly (string Label, string Detail) CrowdAuction = ("crowd auction",
		"pools.trade Crowd Launch: a four-hour continuous clearing auction with a 10k USD FDV floor before the pool opens. Price discovery precedes trading, which is harder to rug-launch.");

	/// <summary>
	/// Uniswap Liquidity Launchpad strategy contracts on Robinhood Chain (developers.uniswap.org
	/// deployments page, 2026-09-03), mapped to the launch format they implement. Unknown strategies
	/// simply produce no format flag.
	/// </summary>
	private static readonly Dictionary<string, (string Label, string Detail)> KnownStrategies =
		new(StringComparer.OrdinalIgnoreCase)
		{
			["0x23f8209572b4a1c2ad88a42749e830791fb027f1"] = InstantLaunch,
			["0xad44d55e7f8337c3ce113fbb591486e85be104b2"] = InstantLaunch,
			["0x000000001f26a0044baa66024e7b6599c61963f8"] = CrowdAuction,
			["0x00cca200bf124dbfa848937c553864f4b4ce0632"] = CrowdAuction,
			["0x05d552391067389ee44fec3924157ed33f976000"] = ("LBP launch",
				"Liquidity bootstrapping pool strategy: the price starts high and decays, discouraging snipers before the pool settles."),
		};

	public static LaunchAssessmentResult Assess(LaunchFacts facts)
	{
		List<LaunchFlagResponse> flags = [];
		bool nativeQuote = facts.PairTokenAddress is null or NativeQuote;

		if (facts.LaunchpadName is { } launchpad)
		{
			flags.Add(VenueFlag(launchpad));
		}

		if (facts.LaunchStrategyAddress is { } strategy && KnownStrategies.TryGetValue(
			strategy,
			out (string Label, string Detail) format))
		{
			flags.Add(new LaunchFlagResponse("launch-format", LaunchFlagSeverity.Info, format.Label, format.Detail));
		}

		if (facts.InsiderCount > 0)
		{
			flags.Add(
				new LaunchFlagResponse(
					"insiders",
					LaunchFlagSeverity.Danger,
					$"{facts.InsiderCount} tax-exempt insider{(facts.InsiderCount == 1 ? string.Empty : "s")}",
					$"The creator exempted {facts.InsiderCount} address{(facts.InsiderCount == 1 ? string.Empty : "es")} from the launch snipe tax, so they buy untaxed in the first second while everyone else pays the penalty. In the field study every such wallet bought instantly and dumped, and they shared a funder with the creator."));
		}

		if (facts.OtherLaunchesByDeployer > 0)
		{
			bool reject = facts.OtherLaunchesByDeployer >= SerialLauncherRejectThreshold;
			flags.Add(
				new LaunchFlagResponse(
					"serial-launcher",
					reject ? LaunchFlagSeverity.Danger : LaunchFlagSeverity.Warning,
					$"serial launcher ({facts.OtherLaunchesByDeployer} other)",
					$"This deployer has {facts.OtherLaunchesByDeployer} other token launch{(facts.OtherLaunchesByDeployer == 1 ? string.Empty : "es")} in the archive. Bot-template launchers relaunch the same name within seconds and never attract organic trades."));
		}

		if (!nativeQuote)
		{
			flags.Add(
				new LaunchFlagResponse(
					"token-quote",
					LaunchFlagSeverity.Warning,
					"token-quoted",
					$"Priced in token {facts.PairTokenAddress} rather than native ETH. A clean-looking launch can still be a trap through its quote asset when that token is self-issued by the same team (JINQIAN/FAMI). Verify the quote is an official Robinhood stock token."));
		}

		if (facts.CreatorBuyQuote is { } buy)
		{
			// The buy is denominated in the quote asset. Only the native case has known decimals,
			// a token quote stays in smallest units until the quote registry knows its decimals.
			flags.Add(
				new LaunchFlagResponse(
					"creator-buy",
					LaunchFlagSeverity.Info,
					nativeQuote ? $"creator buy {FormatNative(buy)} ETH" : "creator buy in quote token",
					(nativeQuote
						? "The creator's own launch buy from the launch calldata."
						: $"The creator's own launch buy from the launch calldata: {buy.ToString(CultureInfo.InvariantCulture)} smallest units of the quote token (its decimals are not known yet).") +
					" Watch for it being sold within the first minute - in the field study the creator exited 100% at T+7s to T+34s on every dump, before any human buyer arrived."));
		}

		LaunchVerdict verdict = flags.Any(flag => flag.Severity == LaunchFlagSeverity.Danger) ? LaunchVerdict.Reject :
			flags.Any(flag => flag.Severity == LaunchFlagSeverity.Warning) ? LaunchVerdict.Watch : LaunchVerdict.Clear;

		return new LaunchAssessmentResult(verdict, flags);
	}

	/// <summary>
	/// The venue is the strongest T+0 prior in the field study: every traced Pons launch ended in a
	/// dump, every runner was a plain Uniswap pool - so Pons is a warning and the Uniswap launchpad
	/// is merely informational.
	/// </summary>
	private static LaunchFlagResponse VenueFlag(string launchpad) =>
		launchpad switch
		{
			"pons" => new LaunchFlagResponse(
				"launchpad",
				LaunchFlagSeverity.Warning,
				"pons launch",
				"Created through the Pons launchpad. Its guaranteed creator first-buy and snipe-tax whitelist select for extractors: every traced Pons launch ended -46% to -100%, while the runners were plain pools."),
			"uniswap" => new LaunchFlagResponse(
				"launchpad",
				LaunchFlagSeverity.Info,
				"pools.trade launch",
				"Created through the Uniswap Liquidity Launchpad (pools.trade): fixed 1B supply into a permanently locked v4 pool. Every runner in the field study launched this way - venue is the strongest T+0 prior, but not a guarantee."),
			_ => new LaunchFlagResponse(
				"launchpad",
				LaunchFlagSeverity.Info,
				$"{launchpad} launch",
				$"Created through the {launchpad} launchpad. No outcome data for this venue yet."),
		};

	private static string FormatNative(BigInteger amount)
	{
		decimal whole = (decimal)amount / (decimal)BigInteger.Pow(10, NativeDecimals);

		return whole.ToString("0.####", CultureInfo.InvariantCulture);
	}
}
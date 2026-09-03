using System.Globalization;
using System.Numerics;
using Argus.Api;
using Argus.Shared;

namespace Argus.UnitTests;

public sealed class LaunchAssessmentTests
{
	[Fact]
	public void PonsLaunchWithInsidersIsRejected()
	{
		LaunchAssessmentResult result = LaunchAssessment.Assess(Facts(launchpad: "pons", insiders: 14));

		Assert.Equal(LaunchVerdict.Reject, result.Verdict);
		LaunchFlagResponse insiders = Assert.Single(result.Flags, flag => flag.Code == "insiders");
		Assert.Equal(LaunchFlagSeverity.Danger, insiders.Severity);
		Assert.Equal("14 tax-exempt insiders", insiders.Label);
	}

	[Fact]
	public void PonsLaunchWithoutInsidersIsOnlyWatched()
	{
		LaunchAssessmentResult result = LaunchAssessment.Assess(Facts(launchpad: "pons"));

		Assert.Equal(LaunchVerdict.Watch, result.Verdict);
		LaunchFlagResponse launchpad = Assert.Single(result.Flags);
		Assert.Equal("launchpad", launchpad.Code);
		Assert.Equal(LaunchFlagSeverity.Warning, launchpad.Severity);
	}

	[Fact]
	public void OneOtherLaunchBySameDeployerIsAWarning()
	{
		LaunchAssessmentResult result = LaunchAssessment.Assess(Facts(otherLaunches: 1));

		Assert.Equal(LaunchVerdict.Watch, result.Verdict);
		LaunchFlagResponse serial = Assert.Single(result.Flags);
		Assert.Equal("serial-launcher", serial.Code);
		Assert.Equal(LaunchFlagSeverity.Warning, serial.Severity);
		Assert.Equal("serial launcher (1 other)", serial.Label);
	}

	[Fact]
	public void SerialLauncherAtThresholdIsRejected()
	{
		LaunchAssessmentResult result = LaunchAssessment.Assess(
			Facts(otherLaunches: LaunchAssessment.SerialLauncherRejectThreshold));

		Assert.Equal(LaunchVerdict.Reject, result.Verdict);
		Assert.Equal(LaunchFlagSeverity.Danger, Assert.Single(result.Flags).Severity);
	}

	[Fact]
	public void TokenQuoteIsWatchedButNativeQuoteIsNotFlagged()
	{
		LaunchAssessmentResult token =
			LaunchAssessment.Assess(Facts(pairToken: "0x90f79bf6eb2c4f870365e785982e1f101e93b906"));
		LaunchAssessmentResult native =
			LaunchAssessment.Assess(Facts(pairToken: "0x0000000000000000000000000000000000000000"));

		Assert.Equal(LaunchVerdict.Watch, token.Verdict);
		Assert.Equal("token-quote", Assert.Single(token.Flags).Code);

		Assert.Equal(LaunchVerdict.Clear, native.Verdict);
		Assert.Empty(native.Flags);
	}

	[Fact]
	public void CreatorBuyIsInformationalAndShownInWholeEth()
	{
		LaunchAssessmentResult result = LaunchAssessment.Assess(
			Facts(creatorBuy: BigInteger.Parse("125000000000000000", CultureInfo.InvariantCulture)));

		Assert.Equal(LaunchVerdict.Clear, result.Verdict);
		LaunchFlagResponse buy = Assert.Single(result.Flags);
		Assert.Equal(LaunchFlagSeverity.Info, buy.Severity);
		Assert.Equal("creator buy 0.125 ETH", buy.Label);
	}

	[Fact]
	public void NothingFlaggedIsClear()
	{
		LaunchAssessmentResult result = LaunchAssessment.Assess(Facts());

		Assert.Equal(LaunchVerdict.Clear, result.Verdict);
		Assert.Empty(result.Flags);
	}

	[Fact]
	public void CreatorBuyInATokenQuoteIsNotLabelledAsEth()
	{
		LaunchAssessmentResult result = LaunchAssessment.Assess(
			Facts(
				pairToken: "0x90f79bf6eb2c4f870365e785982e1f101e93b906",
				creatorBuy: BigInteger.Parse("1704200000000000000", CultureInfo.InvariantCulture)));

		LaunchFlagResponse buy = Assert.Single(result.Flags, flag => flag.Code == "creator-buy");
		Assert.Equal("creator buy in quote token", buy.Label);
		Assert.Contains("1704200000000000000", buy.Detail, StringComparison.Ordinal);
	}

	[Fact]
	public void PonsVenueIsAWarningButPoolsTradeVenueIsInformational()
	{
		LaunchAssessmentResult pons = LaunchAssessment.Assess(Facts(launchpad: "pons"));
		LaunchAssessmentResult uniswap = LaunchAssessment.Assess(Facts(launchpad: "uniswap"));

		Assert.Equal(LaunchVerdict.Watch, pons.Verdict);
		Assert.Equal(LaunchVerdict.Clear, uniswap.Verdict);
		LaunchFlagResponse venue = Assert.Single(uniswap.Flags);
		Assert.Equal("launchpad", venue.Code);
		Assert.Equal(LaunchFlagSeverity.Info, venue.Severity);
		Assert.Equal("pools.trade launch", venue.Label);
	}

	[Fact]
	public void KnownInstantLaunchStrategyYieldsAFormatFlag()
	{
		LaunchAssessmentResult result = LaunchAssessment.Assess(
			Facts(launchpad: "uniswap", strategy: "0x23F8209572b4a1C2AD88A42749E830791Fb027f1"));

		LaunchFlagResponse format = Assert.Single(result.Flags, flag => flag.Code == "launch-format");
		Assert.Equal("instant launch", format.Label);
		Assert.Equal(LaunchVerdict.Clear, result.Verdict);
	}

	[Fact]
	public void UnknownStrategyProducesNoFormatFlag()
	{
		LaunchAssessmentResult result = LaunchAssessment.Assess(
			Facts(launchpad: "uniswap", strategy: "0x60d73b21cdf2ea846ab3d58699bbbb8f29d72491"));

		Assert.DoesNotContain(result.Flags, flag => flag.Code == "launch-format");
	}

	private static LaunchFacts Facts(
		string? launchpad = null,
		int insiders = 0,
		string? pairToken = null,
		BigInteger? creatorBuy = null,
		int otherLaunches = 0,
		string? strategy = null) =>
		new(launchpad, insiders, pairToken, creatorBuy, otherLaunches, strategy);
}
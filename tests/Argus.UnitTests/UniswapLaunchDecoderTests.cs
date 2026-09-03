using System.Globalization;
using System.Numerics;
using Argus.Worker;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Argus.UnitTests;

/// <summary>
/// Fixtures are the real inner calls of pools.trade launch transactions on Robinhood Chain
/// (Blockscout, 2026-09-03), re-wrapped in a multicall - so the decoder is checked against
/// production calldata, not a hand-made approximation of it.
/// </summary>
public sealed class UniswapLaunchDecoderTests
{
	[Fact]
	public void DecodesInstantLaunchWithCreatorFirstBuy()
	{
		// Hookr.fun (HOOKR): createToken + distributeToken (InstantLaunchStrategy v3.2.0) + distributeWithNative.
		UniswapLaunch? launch = UniswapLaunchDecoder.TryDecode(MulticallFromFixture("pools-trade-hookr-multicall.txt"));

		Assert.NotNull(launch);
		Assert.Equal("Hookr.fun", launch.TokenName);
		Assert.Equal("HOOKR", launch.TokenSymbol);
		Assert.Equal("0x23f8209572b4a1c2ad88a42749e830791fb027f1", launch.StrategyAddress);
		Assert.Equal(
			BigInteger.Parse("01d6068e43ac1da7", NumberStyles.HexNumber, CultureInfo.InvariantCulture),
			launch.CreatorBuyNative);
	}

	[Fact]
	public void DecodesLaunchWithoutCreatorBuy()
	{
		// FRONG: createToken + distributeToken only, through the older launcher deployment.
		UniswapLaunch? launch = UniswapLaunchDecoder.TryDecode(MulticallFromFixture("pools-trade-frong-multicall.txt"));

		Assert.NotNull(launch);
		Assert.Equal("frong", launch.TokenName);
		Assert.Equal("FRONG", launch.TokenSymbol);
		Assert.Equal("0x60d73b21cdf2ea846ab3d58699bbbb8f29d72491", launch.StrategyAddress);
		Assert.Null(launch.CreatorBuyNative);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("0x")]
	[InlineData("0xdeadbeef")]
	public void IgnoresNonLaunchInput(string? input) => Assert.Null(UniswapLaunchDecoder.TryDecode(input));

	[Fact]
	public void IgnoresMulticallWithoutLaunchCalls()
	{
		LiquidityLauncherMulticallFunction multicall = new() { Data = ["0xdeadbeef".HexToByteArray(),], };

		Assert.Null(UniswapLaunchDecoder.TryDecode(multicall.GetCallData().ToHex(true)));
	}

	private static string MulticallFromFixture(string fileName)
	{
		string[] innerCalls = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName));

		LiquidityLauncherMulticallFunction multicall = new()
		{
			Data = [.. innerCalls.Where(line => line.Length > 0).Select(line => line.Trim().HexToByteArray()),],
		};

		return multicall.GetCallData().ToHex(true);
	}
}
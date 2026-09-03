using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Argus.Worker;

/// <summary>
/// Decodes a LiquidityLauncher.multicall launch transaction (Uniswap Liquidity Launchpad, the
/// contracts behind pools.trade) into the facts the launch rules use. Pure so the real calldata of
/// observed launches can serve as test fixtures.
/// </summary>
public static class UniswapLaunchDecoder
{
	public static UniswapLaunch? TryDecode(string? input)
	{
		if (input is null || !HasSelector<LiquidityLauncherMulticallFunction>(input))
		{
			return null;
		}

		LiquidityLauncherMulticallFunction multicall = new LiquidityLauncherMulticallFunction().DecodeInput(input);

		string? name = null;
		string? symbol = null;
		string? strategy = null;
		System.Numerics.BigInteger? creatorBuy = null;
		bool sawLaunchCall = false;

		foreach (byte[] inner in multicall.Data)
		{
			string call = inner.ToHex(true);

			if (HasSelector<LauncherCreateTokenFunction>(call))
			{
				LauncherCreateTokenFunction created = new LauncherCreateTokenFunction().DecodeInput(call);
				name = created.Name;
				symbol = created.Symbol;
				sawLaunchCall = true;
			}
			else if (HasSelector<LauncherDistributeTokenFunction>(call))
			{
				LauncherDistributeTokenFunction distributed = new LauncherDistributeTokenFunction().DecodeInput(call);
				strategy = distributed.Distribution.Strategy.ToLowerInvariant();
				sawLaunchCall = true;
			}
			else if (HasSelector<LauncherDistributeWithNativeFunction>(call))
			{
				LauncherDistributeWithNativeFunction bought =
					new LauncherDistributeWithNativeFunction().DecodeInput(call);
				creatorBuy = bought.NativeAmount;
			}
		}

		return sawLaunchCall ? new UniswapLaunch(name, symbol, strategy, creatorBuy) : null;
	}

	private static bool HasSelector<TFunction>(string callData)
		where TFunction : FunctionMessage
	{
		string selector = ABITypedRegistry.GetFunctionABI<TFunction>().Sha3Signature;

		return callData.Length >= 10 && string.Equals(
			callData.Substring(2, 8),
			selector,
			StringComparison.OrdinalIgnoreCase);
	}
}
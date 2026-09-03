using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Argus.Data.Entities;

/// <summary>
/// A contract deployment observed on an EVM chain. Hashes and addresses are stored lowercased.
/// One launch usually yields several rows sharing a <see cref="TransactionHash" /> - a Pons
/// launchAndBuy creates the token and its curve - so consumers that must act once per launch
/// (alert rules) key on the ERC-20 row, the one with a non-null <see cref="TokenSymbol" />.
/// </summary>
[Index(nameof(ChainId), nameof(ContractAddress), IsUnique = true)]
public sealed class TokenDeployment
{
	public long Id { get; set; }

	public long ChainId { get; set; }

	public long BlockNumber { get; set; }

	public required string BlockHash { get; set; }

	public required string TransactionHash { get; set; }

	public required string ContractAddress { get; set; }

	public required string DeployerAddress { get; set; }

	/// <summary>
	/// Gets or sets the contract that performed the deployment (launchpad/factory), or null for
	/// a plain top-level CREATE. One transaction can create multiple contracts.
	/// </summary>
	public string? FactoryAddress { get; set; }

	/// <summary>
	/// Gets or sets the configured name of the launchpad behind <see cref="FactoryAddress" />
	/// (e.g. "pons"), or null when the factory is not a known launchpad.
	/// </summary>
	public string? LaunchpadName { get; set; }

	/// <summary>
	/// Gets or sets the ERC-20 name, or null when the contract exposes no readable metadata
	/// (i.e. it is probably not a token).
	/// </summary>
	public string? TokenName { get; set; }

	public string? TokenSymbol { get; set; }

	public int? TokenDecimals { get; set; }

	/// <summary>
	/// Gets or sets the quote asset the launch was paired against, decoded from the launch
	/// calldata (known launchpads only). The zero address means the launch is quoted in the
	/// chain's native currency, which is canonical - distinct from null, which means no launch
	/// calldata was decoded at all. Scoring only treats an unrecognized *token* quote as a flag.
	/// </summary>
	public string? PairTokenAddress { get; set; }

	/// <summary>
	/// Gets or sets the creator's own launch buy in the quote asset's smallest unit, decoded
	/// from the launch calldata (known launchpads only).
	/// </summary>
	public BigInteger? CreatorBuyQuote { get; set; }

	/// <summary>
	/// Gets or sets the launch strategy contract that distributed the token (Uniswap Liquidity
	/// Launchpad: instant launch, crowd auction, LBP), decoded from the launch calldata. The format
	/// is classified at read time from a known-strategy map so new strategies need no migration.
	/// </summary>
	public string? LaunchStrategyAddress { get; set; }

	public DateTimeOffset DetectedAt { get; set; }

	public ICollection<TokenDeploymentInsider> Insiders { get; set; } = [];
}
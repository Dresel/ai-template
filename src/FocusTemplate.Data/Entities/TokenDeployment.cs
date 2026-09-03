using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data.Entities;

/// <summary>
/// A contract deployment observed on an EVM chain. Hashes and addresses are stored lowercased.
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

	public DateTimeOffset DetectedAt { get; set; }
}
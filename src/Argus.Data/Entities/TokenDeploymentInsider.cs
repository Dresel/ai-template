using Microsoft.EntityFrameworkCore;

namespace Argus.Data.Entities;

/// <summary>
/// An address the creator whitelisted in the launch transaction (e.g. Pons snipeTaxExemptions) -
/// insiders that can buy in the first blocks without paying the launch snipe tax. Addresses are
/// stored lowercased.
/// </summary>
[Index(nameof(TokenDeploymentId), nameof(Address), IsUnique = true)]
public sealed class TokenDeploymentInsider
{
	public long Id { get; set; }

	public long TokenDeploymentId { get; set; }

	public TokenDeployment? TokenDeployment { get; set; }

	public required string Address { get; set; }
}
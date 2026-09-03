using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data.Entities;

/// <summary>
/// Per-chain ingest progress: the last block the deployment listener fully processed.
/// Lets a restarted listener resume where it stopped instead of skipping to the tip.
/// </summary>
[PrimaryKey(nameof(ChainId))]
public sealed class ChainCursor
{
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public long ChainId { get; set; }

	public long LastProcessedBlock { get; set; }

	public DateTimeOffset UpdatedAt { get; set; }
}
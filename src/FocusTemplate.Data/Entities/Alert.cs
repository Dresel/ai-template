using System.ComponentModel.DataAnnotations;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Entities;

public sealed class Alert : IAuditable
{
	public required AlertId Id { get; init; }

	public required StationId StationId { get; init; }

	public required ObservationId ObservationId { get; init; }

	public required AlertKind Kind { get; init; }

	[MaxLength(500)]
	public required string Message { get; init; }

	public required DateTimeOffset RaisedAt { get; init; }

	public DateTimeOffset? ResolvedAt { get; set; }
}
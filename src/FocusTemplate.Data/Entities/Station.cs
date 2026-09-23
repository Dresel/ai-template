using System.ComponentModel.DataAnnotations;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;
using NetTopologySuite.Geometries;

namespace FocusTemplate.Data.Entities;

public sealed class Station : IAuditable
{
	public required StationId Id { get; init; }

	public required UserId OwnerId { get; init; }

	[MaxLength(32)]
	public required string Code { get; set; }

	[MaxLength(200)]
	public required string Name { get; set; }

	[MaxLength(2000)]
	public string? Description { get; set; }

	public required Point Location { get; set; }

	public StationStatus Status { get; set; } = StationStatus.Active;

	public int? MinTemperatureC { get; set; }

	public int? MaxTemperatureC { get; set; }
}
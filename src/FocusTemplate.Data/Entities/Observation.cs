using System.ComponentModel.DataAnnotations;
using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Entities;

public sealed class Observation
{
	public ObservationId Id { get; set; } = ObservationId.Unspecified;

	public required StationId StationId { get; init; }

	public required DateTimeOffset MeasuredAt { get; init; }

	[Range(-100.0, 100.0)]
	public required double TemperatureC { get; init; }

	[Range(0, 100)]
	public required int HumidityPercent { get; init; }

	[Range(800.0, 1200.0)]
	public required double PressureHpa { get; init; }
}
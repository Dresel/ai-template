using SpatialFocus.Web.Bff.Shared;

namespace SpatialFocus.Web.Bff.Hosting;

public sealed class ClientConfiguration
{
	public AnalyticsConfiguration Analytics { get; init; } = new();
}
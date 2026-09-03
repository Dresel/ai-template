using Argus.Shared;

namespace Argus.Web.Bff;

public sealed class ClientConfiguration
{
	public AnalyticsConfiguration Analytics { get; init; } = new();
}
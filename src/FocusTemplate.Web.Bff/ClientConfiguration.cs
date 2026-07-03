using FocusTemplate.Shared;

namespace FocusTemplate.Web.Bff;

public sealed class ClientConfiguration
{
	public AnalyticsConfiguration Analytics { get; init; } = new();
}
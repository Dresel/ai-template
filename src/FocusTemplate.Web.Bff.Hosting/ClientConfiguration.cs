using FocusTemplate.Web.Bff.Shared;

namespace FocusTemplate.Web.Bff.Hosting;

public sealed class ClientConfiguration
{
	public AnalyticsConfiguration Analytics { get; init; } = new();
}
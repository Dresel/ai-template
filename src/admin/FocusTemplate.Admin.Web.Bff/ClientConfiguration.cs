using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Web.Bff;

public sealed class ClientConfiguration
{
	public AnalyticsConfiguration Analytics { get; init; } = new();
}
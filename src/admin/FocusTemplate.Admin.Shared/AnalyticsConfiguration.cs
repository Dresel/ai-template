namespace FocusTemplate.Admin.Shared;

public sealed class AnalyticsConfiguration
{
	public string WebsiteId { get; init; } = string.Empty;

	public string Provider { get; init; } = string.Empty;

	public string ScriptUrl { get; init; } = string.Empty;
}
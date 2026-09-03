using System.Text.Json.Serialization;

namespace Argus.Shared;

/// <summary>
/// How loudly a launch flag should be shown. Serialized by name so the wire contract stays
/// readable and stable if values are reordered.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<LaunchFlagSeverity>))]
public enum LaunchFlagSeverity
{
	Info,
	Warning,
	Danger,
}
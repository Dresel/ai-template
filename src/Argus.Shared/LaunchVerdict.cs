using System.Text.Json.Serialization;

namespace Argus.Shared;

/// <summary>
/// The T+0 outcome of the deterministic launch rules. Clear means no flag fired - not an
/// endorsement, since the lifecycle checks (creator or insider selling) only run after launch.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<LaunchVerdict>))]
public enum LaunchVerdict
{
	Clear,
	Watch,
	Reject,
}
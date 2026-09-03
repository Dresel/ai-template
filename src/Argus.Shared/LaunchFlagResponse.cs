namespace Argus.Shared;

/// <summary>
/// One detector-backed flag on a launch: a stable machine code, the severity the UI colors by,
/// a short chip label, and the tooltip text explaining the mechanism and the evidence.
/// </summary>
public sealed record LaunchFlagResponse(string Code, LaunchFlagSeverity Severity, string Label, string Detail);
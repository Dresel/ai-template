using Argus.Shared;

namespace Argus.Api;

public sealed record LaunchAssessmentResult(LaunchVerdict Verdict, IReadOnlyList<LaunchFlagResponse> Flags);
using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Client;

// A modeled problem the caller left to the failure path, a status the contract does not model (no problem), or no answer at all (no status either)
public sealed record ApiFailure(int? Status, ProblemDetails? Problem);
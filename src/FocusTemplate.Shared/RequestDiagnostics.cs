namespace FocusTemplate.Shared;

public sealed record RequestDiagnostics(
	string Scheme,
	bool IsHttps,
	string Host,
	string? RemoteIp,
	string? XForwardedProto,
	string? XForwardedHost,
	string? XForwardedFor,
	string? XOriginalProto,
	string? XOriginalFor);
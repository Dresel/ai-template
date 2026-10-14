using Aspire.Hosting.ApplicationModel;

namespace SpatialFocus.AppHost.Hosting.Authentication;

public sealed record KeycloakRealm(
	IResourceBuilder<IResource> Server,
	ReferenceExpression Authority,
	ReferenceExpression ApiAudience,
	ReferenceExpression BffClientId,
	IResourceBuilder<ParameterResource> BffClientSecret,
	string? LoginHint = null);
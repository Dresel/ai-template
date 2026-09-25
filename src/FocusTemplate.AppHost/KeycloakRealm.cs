namespace FocusTemplate.AppHost;

internal sealed record KeycloakRealm(
	IResourceBuilder<IResource> Server,
	ReferenceExpression Authority,
	ReferenceExpression AdminApiAudience,
	ReferenceExpression AdminBffClientId,
	IResourceBuilder<ParameterResource> AdminBffClientSecret);
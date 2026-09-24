namespace FocusTemplate.AppHost;

internal static class AuthenticationExtensions
{
	// The realm keycloak/focus-realm.json imports, which is also where the client ids and audiences handed in below are declared.
	private const string Realm = "focus";

	public static IResourceBuilder<ProjectResource> WithKeycloakAudience(
		this IResourceBuilder<ProjectResource> api,
		IResourceBuilder<KeycloakResource> keycloak,
		string audience) =>
		api.WithKeycloakAuthority(keycloak).WithEnvironment("Oidc__Audience", audience);

	public static IResourceBuilder<ProjectResource> WithKeycloakClient(
		this IResourceBuilder<ProjectResource> app,
		IResourceBuilder<KeycloakResource> keycloak,
		string clientId,
		IResourceBuilder<ParameterResource> clientSecret) =>
		app.WithKeycloakAuthority(keycloak)
			.WithEnvironment("Oidc__ClientId", clientId)
			.WithEnvironment("Oidc__ClientSecret", clientSecret);

	private static IResourceBuilder<ProjectResource> WithKeycloakAuthority(
		this IResourceBuilder<ProjectResource> app,
		IResourceBuilder<KeycloakResource> keycloak) =>
		app.WithEnvironment(
				"Oidc__Authority",
				ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/{Realm}"))
			.WithReference(keycloak)
			.WaitFor(keycloak);
}
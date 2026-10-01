namespace FocusTemplate.AppHost;

internal static class AuthenticationExtensions
{
	// The realm keycloak/focus-realm.json imports, which is also where its client, audience and secret are declared.
	private const string Realm = "focus";

	public static KeycloakRealm AddExternalKeycloakRealm(this IDistributedApplicationBuilder builder)
	{
		IResourceBuilder<ParameterResource> authority = builder.AddParameter("oidc-authority")
			.WithDescription("The realm's URL, such as `https://sso.example.com/realms/focus`.", true);
		IResourceBuilder<ParameterResource> audience = builder.AddParameter(
			"oidc-admin-api-audience",
			"admin-api",
			true);
		IResourceBuilder<ParameterResource> clientId = builder.AddParameter(
			"oidc-admin-bff-client-id",
			"admin-bff",
			true);

		return new KeycloakRealm(
			builder.AddExternalService("keycloak", authority).WithHttpHealthCheck(),
			ReferenceExpression.Create($"{authority}"),
			ReferenceExpression.Create($"{audience}"),
			ReferenceExpression.Create($"{clientId}"),
			builder.AddParameter("oidc-admin-bff-secret", true));
	}

	public static KeycloakRealm AddLocalKeycloakRealm(this IDistributedApplicationBuilder builder, bool persistent)
	{
		IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak", 8080)
			.WithOtlpExporter()
			.WithRealmImport("./keycloak");

		if (persistent)
		{
			// No wait for the JVM and the realm import on the next start, and the developer's session survives it. Keycloak
			// skips importing a realm that already exists, so a changed focus-realm.json needs `aspire stop --force --volumes`.
			keycloak.WithLifetime(ContainerLifetime.Persistent).WithDataVolume();
		}

		return new KeycloakRealm(
			keycloak,
			ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/{Realm}"),
			ReferenceExpression.Create($"admin-api"),
			ReferenceExpression.Create($"admin-bff"),
			builder.AddParameter("oidc-admin-bff-secret", "admin-bff-secret", secret: true));
	}

	public static IResourceBuilder<ProjectResource> WithKeycloakAudience(
		this IResourceBuilder<ProjectResource> api,
		KeycloakRealm realm) =>
		api.WithKeycloakAuthority(realm).WithEnvironment("Oidc__Audience", realm.AdminApiAudience);

	public static IResourceBuilder<ProjectResource> WithKeycloakClient(
		this IResourceBuilder<ProjectResource> app,
		KeycloakRealm realm) =>
		app.WithKeycloakAuthority(realm)
			.WithEnvironment("Oidc__ClientId", realm.AdminBffClientId)
			.WithEnvironment("Oidc__ClientSecret", realm.AdminBffClientSecret);

	private static IResourceBuilder<ProjectResource> WithKeycloakAuthority(
		this IResourceBuilder<ProjectResource> app,
		KeycloakRealm realm) =>
		app.WithEnvironment("Oidc__Authority", realm.Authority).WaitFor(realm.Server);
}
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace FocusTemplate.AppHost.Hosting.Authentication;

// The realm's audience is named after the API's resource and its client after the BFF's, and so are their parameters
public static class DistributedApplicationBuilderExtensions
{
	private const string DeveloperLogin = "developer";

	public static KeycloakRealm AddExternalKeycloakRealm(this IDistributedApplicationBuilder builder, string name, string api, string bff)
	{
		IResourceBuilder<ParameterResource> authority = builder.AddParameter("oidc-authority")
			.WithDescription("The realm's URL, such as `https://sso.example.com/realms/focus`.", true);
		IResourceBuilder<ParameterResource> audience = builder.AddParameter($"oidc-{api}-audience", api, true);
		IResourceBuilder<ParameterResource> clientId = builder.AddParameter($"oidc-{bff}-client-id", bff, true);

		return new KeycloakRealm(
			builder.AddExternalService(name, authority).WithHttpHealthCheck(),
			ReferenceExpression.Create($"{authority}"),
			ReferenceExpression.Create($"{audience}"),
			ReferenceExpression.Create($"{clientId}"),
			builder.AddParameter($"oidc-{bff}-secret", true));
	}

	// Imports the realm file in the AppHost's keycloak folder, which declares the BFF's client with the secret
	// `<bff>-secret`, the audience mapper for the API and the login `developer`.
	public static KeycloakRealm AddLocalKeycloakRealm(
		this IDistributedApplicationBuilder builder,
		string name,
		int port,
		string realm,
		string api,
		string bff,
		bool persistent)
	{
		IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak(name, port)
			.WithOtlpExporter()
			.WithRealmImport("./keycloak");

		if (persistent)
		{
			// No wait for the JVM and the realm import on the next start, and the developer's session survives it. Keycloak
			// skips importing a realm that already exists, so a changed realm file needs `aspire stop --force --volumes`.
			// The proxy keeps `localhost` answering on ::1 as well, see docs/apphost.md.
			keycloak.WithLifetime(ContainerLifetime.Persistent)
				.WithDataVolume()
				.WithEndpoint("http", endpoint => endpoint.IsProxied = true);
		}

		return new KeycloakRealm(
			keycloak,
			ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/{realm}"),
			ReferenceExpression.Create($"{api}"),
			ReferenceExpression.Create($"{bff}"),
			builder.AddParameter($"oidc-{bff}-secret", $"{bff}-secret", secret: true),
			DeveloperLogin);
	}
}
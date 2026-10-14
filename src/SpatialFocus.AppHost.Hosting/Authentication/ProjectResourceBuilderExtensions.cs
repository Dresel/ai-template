using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace SpatialFocus.AppHost.Hosting.Authentication;

public static class ProjectResourceBuilderExtensions
{
	public static IResourceBuilder<ProjectResource> WithKeycloakAudience(this IResourceBuilder<ProjectResource> api, KeycloakRealm realm) =>
		api.WithKeycloakAuthority(realm).WithEnvironment("Oidc__Audience", realm.ApiAudience);

	public static IResourceBuilder<ProjectResource> WithKeycloakClient(this IResourceBuilder<ProjectResource> app, KeycloakRealm realm)
	{
		app.WithKeycloakAuthority(realm)
			.WithEnvironment("Oidc__ClientId", realm.BffClientId)
			.WithEnvironment("Oidc__ClientSecret", realm.BffClientSecret);

		return realm.LoginHint is { } loginHint ? app.WithEnvironment("Oidc__LoginHint", loginHint) : app;
	}

	private static IResourceBuilder<ProjectResource> WithKeycloakAuthority(
		this IResourceBuilder<ProjectResource> app,
		KeycloakRealm realm) =>
		app.WithEnvironment("Oidc__Authority", realm.Authority).WaitFor(realm.Server);
}
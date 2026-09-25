using Projects;

namespace FocusTemplate.Admin.Web.E2E;

// Only the AppHost's model: nothing starts, so these need neither Docker nor a Keycloak.
public sealed class KeycloakRealmTests
{
	[Fact]
	public async Task PublishingTakesTheRealmFromParametersEvenWithTheLocalKeycloakOn()
	{
		await using IDistributedApplicationTestingBuilder builder =
			await CreateAsync("--operation", "publish", "Features:LocalKeycloak=true");

		Assert.True(builder.ExecutionContext.IsPublishMode);
		await AssertRealmFromParametersAsync(builder);

		// The local realm's client secret is known to everyone who has the repository.
		ParameterResource secret = builder.Resources.OfType<ParameterResource>()
			.Single(parameter => parameter.Name == "oidc-admin-bff-secret");

		Assert.True(secret.Secret);
		Assert.Null(secret.Default);
	}

	[Fact]
	public async Task TurningTheLocalKeycloakOffTakesTheRealmFromParameters()
	{
		await using IDistributedApplicationTestingBuilder builder = await CreateAsync("Features:LocalKeycloak=false");

		await AssertRealmFromParametersAsync(builder);
	}

	private static async Task AssertRealmFromParametersAsync(IDistributedApplicationTestingBuilder builder)
	{
		Assert.Empty(builder.Resources.OfType<KeycloakResource>());
		Assert.Equal(
			"oidc-authority",
			Assert.Single(builder.Resources.OfType<ExternalServiceResource>()).UrlParameter?.Name);

		// Built, not started. Evaluated as for publishing, each value is the expression naming its source, so no parameter
		// needs a value.
		await using DistributedApplication app = await builder.BuildAsync(TestContext.Current.CancellationToken);
		DistributedApplicationExecutionContext publishing = new(
			new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish)
			{
				Services = app.Services,
			});

		Assert.Equal(
			new Dictionary<string, string>
			{
				["Oidc__Audience"] = "{oidc-admin-api-audience.value}",
				["Oidc__Authority"] = "{oidc-authority.value}",
			},
			await OidcEnvironmentAsync(publishing, builder, "admin-api"));

		Assert.Equal(
			new Dictionary<string, string>
			{
				["Oidc__Authority"] = "{oidc-authority.value}",
				["Oidc__ClientId"] = "{oidc-admin-bff-client-id.value}",
				["Oidc__ClientSecret"] = "{oidc-admin-bff-secret.value}",
			},
			await OidcEnvironmentAsync(publishing, builder, "admin-bff"));
	}

	private static Task<IDistributedApplicationTestingBuilder> CreateAsync(params string[] args) =>
		DistributedApplicationTestingBuilder.CreateAsync<FocusTemplate_AppHost>(
			["Features:TlsOffloadingIngress=false", "Features:Analytics=false", "Features:Mobile=false", .. args,],
			TestContext.Current.CancellationToken);

	private static async Task<Dictionary<string, string>> OidcEnvironmentAsync(
		DistributedApplicationExecutionContext executionContext,
		IDistributedApplicationTestingBuilder builder,
		string projectName)
	{
		IExecutionConfigurationResult configuration = await ExecutionConfigurationBuilder
			.Create(builder.Resources.OfType<ProjectResource>().Single(project => project.Name == projectName))
			.WithEnvironmentVariablesConfig()
			.BuildAsync(executionContext, null, TestContext.Current.CancellationToken);

		Assert.Null(configuration.Exception);

		return configuration.EnvironmentVariables
			.Where(variable => variable.Key.StartsWith("Oidc__", StringComparison.Ordinal))
			.ToDictionary();
	}
}
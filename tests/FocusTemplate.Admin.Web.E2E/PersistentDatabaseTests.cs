using Projects;

namespace FocusTemplate.Admin.Web.E2E;

// Only the AppHost's model: nothing starts, so these need no Docker.
public sealed class PersistentDatabaseTests
{
	[Fact]
	public async Task ThePersistentDatabaseStaysBehindTheProxyOnAFixedPort()
	{
		await using IDistributedApplicationTestingBuilder builder = await CreateAsync("Features:PersistentDatabase=true");

		EndpointAnnotation endpoint = Postgres(builder)
			.Annotations.OfType<EndpointAnnotation>()
			.Single(annotation => annotation.Name == "tcp");

		Assert.True(endpoint.IsExplicitlyProxied);
		Assert.Equal(15432, endpoint.Port);
	}

	[Fact]
	public async Task ThePostgresIsRecreatedOnEveryStartByDefault()
	{
		await using IDistributedApplicationTestingBuilder builder = await CreateAsync();

		PostgresServerResource postgres = Postgres(builder);

		Assert.False(postgres.TryGetLastAnnotation(out ContainerLifetimeAnnotation? _));
		Assert.DoesNotContain(postgres.Annotations.OfType<ContainerMountAnnotation>(), mount => mount.Type == ContainerMountType.Volume);
	}

	[Fact]
	public async Task TurningThePersistentDatabaseOnKeepsTheContainerAndItsDataBetweenStarts()
	{
		await using IDistributedApplicationTestingBuilder builder = await CreateAsync("Features:PersistentDatabase=true");

		PostgresServerResource postgres = Postgres(builder);

		Assert.True(postgres.TryGetLastAnnotation(out ContainerLifetimeAnnotation? lifetime));
		Assert.Equal(ContainerLifetime.Persistent, lifetime.Lifetime);
		Assert.Contains(postgres.Annotations.OfType<ContainerMountAnnotation>(), mount => mount.Type == ContainerMountType.Volume);
	}

	private static Task<IDistributedApplicationTestingBuilder> CreateAsync(params string[] args) =>
		DistributedApplicationTestingBuilder.CreateAsync<FocusTemplate_AppHost>(
			[
				"Features:TlsOffloadingIngress=false",
				"Features:Analytics=false",
				"Features:Mobile=false",
				"Features:LocalKeycloak=true",
				"Features:PersistentLocalKeycloak=false",
				"Features:PersistentDatabase=false",
				.. args,
			],
			TestContext.Current.CancellationToken);

	// Umami has its own Postgres with a volume of its own, so the name picks the application's.
	private static PostgresServerResource Postgres(IDistributedApplicationTestingBuilder builder) =>
		builder.Resources.OfType<PostgresServerResource>().Single(postgres => postgres.Name == "postgres");
}
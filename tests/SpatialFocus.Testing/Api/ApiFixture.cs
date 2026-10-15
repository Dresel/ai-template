using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Respawn;
using SpatialFocus.Data;
using SpatialFocus.Data.Foundation.Auditing;
using SpatialFocus.Primitives;

namespace SpatialFocus.Testing.Api;

// One API host on a database of its own. The host under test names its connections and permissions and creates the
// schema, from a product's migrations or from the models.
public abstract class ApiFixture<TProgram>(
	PostgresFixture postgres,
	RedisFixture redis,
	string databaseConnectionName,
	string readOnlyDatabaseConnectionName,
	string redisConnectionName) : WebApplicationFactory<TProgram>, IApiFixture, IAsyncLifetime
	where TProgram : class
{
	// One for the host and its replicas, so they share their Redis channels with each other and with no other class
	private readonly string channelPrefix = $"{Guid.NewGuid():N}:";

	private string readOnlyConnectionString = string.Empty;

	private Respawner respawner = null!;

	public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero));

	public string ConnectionString { get; private set; } = string.Empty;

	public abstract IReadOnlyList<Permission> Permissions { get; }

	public AuditingInterceptor CreateAuditing(UserId? user = null) =>
		new(Clock, new FixedCurrentUser(user ?? TestAuthenticationHandler.TestUser));

	public HttpClient CreateAuthenticatedClient(UserId? user = null) => TestAuthenticationHandler.Authenticate(CreateClient(), user);

	public AppDbContext CreateDbContext(UserId? user = null)
	{
		DbContextOptionsBuilder<AppDbContext> options =
			new DbContextOptionsBuilder<AppDbContext>().ConfigureAppDbContext(ConnectionString, CreateAuditing(user));
		ConfigureAppDbContextOptions(options);

		return new AppDbContext(options.Options);
	}

	// Another API process on the same database and Redis channels, as a second replica behind the ingress would be
	public WebApplicationFactory<TProgram> CreateReplica() => WithWebHostBuilder(_ => { });

	public async ValueTask InitializeAsync()
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		ConnectionString = await postgres.CreateDatabaseAsync($"test_{Guid.NewGuid():N}");
		this.readOnlyConnectionString = PostgresFixture.ReadOnlyConnectionString(ConnectionString);

		await CreateSchemaAsync(cancellationToken);

		await PostgresFixture.GrantReadAccessAsync(ConnectionString);

		await using NpgsqlConnection connection = new(ConnectionString);
		await connection.OpenAsync(cancellationToken);
		this.respawner = await Respawner.CreateAsync(
			connection,
			new RespawnerOptions { DbAdapter = DbAdapter.Postgres, TablesToIgnore = ["__EFMigrationsHistory", "permissions",], });
	}

	public async Task ResetAsync()
	{
		await using NpgsqlConnection connection = new(ConnectionString);
		await connection.OpenAsync(TestContext.Current.CancellationToken);
		await this.respawner.ResetAsync(connection);
	}

	protected virtual void ConfigureAppDbContextOptions(DbContextOptionsBuilder<AppDbContext> options)
	{
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseSetting($"ConnectionStrings:{databaseConnectionName}", ConnectionString);
		builder.UseSetting($"ConnectionStrings:{readOnlyDatabaseConnectionName}", this.readOnlyConnectionString);
		builder.UseSetting($"ConnectionStrings:{redisConnectionName}", redis.ConnectionStringFor(this.channelPrefix));

		// The values only have to pass validation: JwtBearer is registered but never asked, the test scheme is the default.
		builder.UseSetting("Oidc:Authority", "https://keycloak.test/realms/focus");
		builder.UseSetting("Oidc:Audience", "api");

		builder.ConfigureServices(services =>
		{
			services.AddSingleton<TimeProvider>(Clock);

			services.AddAuthentication(TestAuthenticationHandler.SchemeName)
				.AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, null);
		});
	}

	// Called once per class before the read grant and the Respawn snapshot, so both see every table
	protected abstract Task CreateSchemaAsync(CancellationToken cancellationToken);
}
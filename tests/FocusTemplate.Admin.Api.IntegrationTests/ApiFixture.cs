using System.Net.Http.Headers;
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
using SpatialFocus.Weather.Data;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class ApiFixture(PostgresFixture postgres, RedisFixture redis) : WebApplicationFactory<Program>, IAsyncLifetime
{
	// One for the host and its replicas, so they share their Redis channels with each other and with no other class
	private readonly string channelPrefix = $"{Guid.NewGuid():N}:";

	private string connectionString = string.Empty;

	private string readOnlyConnectionString = string.Empty;

	private Respawner respawner = null!;

	public static UserId TestUser { get; } = UserId.From(new Guid("00000000-0000-7000-8000-00000000c0de"));

	public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero));

	public static HttpClient Authenticate(HttpClient client, UserId? user = null)
	{
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
			TestAuthenticationHandler.SchemeName,
			(user ?? TestUser).Value.ToString());

		return client;
	}

	public HttpClient CreateAuthenticatedClient(UserId? user = null) => Authenticate(CreateClient(), user);

	public AppDbContext CreateDbContext(UserId? user = null) =>
		new(
			new DbContextOptionsBuilder<AppDbContext>().ConfigureAppDbContext(
					this.connectionString,
					new AuditingInterceptor(Clock, new FixedCurrentUser(user ?? TestUser)))
				.Options);

	// Another API process on the same database and Redis channels, as a second replica behind the ingress would be
	public WebApplicationFactory<Program> CreateReplica() => WithWebHostBuilder(_ => { });

	public WeatherDbContext CreateWeatherDbContext(UserId? user = null) =>
		new(
			new DbContextOptionsBuilder<WeatherDbContext>().ConfigureWeatherDbContext(
					this.connectionString,
					new AuditingInterceptor(Clock, new FixedCurrentUser(user ?? TestUser)))
				.Options);

	public async ValueTask InitializeAsync()
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		this.connectionString = await postgres.CreateDatabaseAsync($"test_{Guid.NewGuid():N}");
		this.readOnlyConnectionString = PostgresFixture.ReadOnlyConnectionString(this.connectionString);

		// In the AppHost's order: the feature's stations point at the spine's users
		await using (AppDbContext dbContext = CreateDbContext())
		{
			await dbContext.Database.MigrateAsync(cancellationToken);
		}

		await using (WeatherDbContext dbContext = CreateWeatherDbContext())
		{
			await dbContext.Database.MigrateAsync(cancellationToken);
		}

		await PostgresFixture.GrantReadAccessAsync(this.connectionString);

		await using NpgsqlConnection connection = new(this.connectionString);
		await connection.OpenAsync(cancellationToken);
		this.respawner = await Respawner.CreateAsync(
			connection,
			new RespawnerOptions { DbAdapter = DbAdapter.Postgres, TablesToIgnore = ["__EFMigrationsHistory", "permissions",], });
	}

	public async Task ResetAsync()
	{
		await using NpgsqlConnection connection = new(this.connectionString);
		await connection.OpenAsync(TestContext.Current.CancellationToken);
		await this.respawner.ResetAsync(connection);
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseSetting("ConnectionStrings:focusdb", this.connectionString);
		builder.UseSetting("ConnectionStrings:focusdb-readonly", this.readOnlyConnectionString);
		builder.UseSetting("ConnectionStrings:redis", redis.ConnectionStringFor(this.channelPrefix));

		// The values only have to pass validation: JwtBearer is registered but never asked, the test scheme is the default.
		builder.UseSetting("Oidc:Authority", "https://keycloak.test/realms/focus");
		builder.UseSetting("Oidc:Audience", "admin-api");

		builder.ConfigureServices(services =>
		{
			services.AddSingleton<TimeProvider>(Clock);

			services.AddAuthentication(TestAuthenticationHandler.SchemeName)
				.AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, null);
		});
	}
}
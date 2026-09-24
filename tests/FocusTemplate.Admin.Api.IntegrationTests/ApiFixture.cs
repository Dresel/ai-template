using System.Net.Http.Headers;
using FocusTemplate.Data;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Respawn;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class ApiFixture(PostgresFixture postgres) : WebApplicationFactory<Program>, IAsyncLifetime
{
	private string connectionString = string.Empty;

	private string readOnlyConnectionString = string.Empty;

	private Respawner respawner = null!;

	public static UserId TestUser { get; } = UserId.From(new Guid("00000000-0000-7000-8000-00000000c0de"));

	public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero));

	public HttpClient CreateAuthenticatedClient(UserId? user = null)
	{
		HttpClient client = CreateClient();
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
			TestAuthenticationHandler.SchemeName,
			(user ?? TestUser).Value.ToString());

		return client;
	}

	public AppDbContext CreateDbContext(UserId? user = null) =>
		new(
			new DbContextOptionsBuilder<AppDbContext>().ConfigureAppDbContext(
					this.connectionString,
					new AuditingInterceptor(Clock, new FixedCurrentUser(user ?? TestUser)))
				.Options);

	public async ValueTask InitializeAsync()
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		this.connectionString = await postgres.CreateDatabaseAsync($"test_{Guid.NewGuid():N}");
		this.readOnlyConnectionString = PostgresFixture.ReadOnlyConnectionString(this.connectionString);

		await using (AppDbContext dbContext = CreateDbContext())
		{
			await dbContext.Database.MigrateAsync(cancellationToken);
		}

		await PostgresFixture.GrantReadAccessAsync(this.connectionString);

		await using NpgsqlConnection connection = new(this.connectionString);
		await connection.OpenAsync(cancellationToken);
		this.respawner = await Respawner.CreateAsync(
			connection,
			new RespawnerOptions { DbAdapter = DbAdapter.Postgres, TablesToIgnore = ["__EFMigrationsHistory",], });
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

		// The values only have to pass validation: JwtBearer is registered but never asked, the test scheme is the default.
		builder.UseSetting("Oidc:Authority", "https://keycloak.test/realms/focus");
		builder.UseSetting("Oidc:Audience", "admin-api");

		builder.ConfigureServices(services =>
		{
			services.AddSingleton<TimeProvider>(Clock);

			services.AddAuthentication(TestAuthenticationHandler.SchemeName)
				.AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
					TestAuthenticationHandler.SchemeName,
					null);
		});
	}
}
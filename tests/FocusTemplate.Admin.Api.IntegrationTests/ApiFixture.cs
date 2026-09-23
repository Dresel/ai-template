using FocusTemplate.Data;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;
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

	private Respawner respawner = null!;

	public static UserId TestUser { get; } = UserId.From(new Guid("00000000-0000-7000-8000-00000000c0de"));

	public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero));

	public AppDbContext CreateDbContext(UserId? user = null) =>
		new(
			new DbContextOptionsBuilder<AppDbContext>()
				.ConfigureAppDbContext(
					this.connectionString,
					new AuditingInterceptor(this.Clock, new FixedCurrentUser(user ?? TestUser)))
				.Options);

	public async ValueTask InitializeAsync()
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		this.connectionString = await postgres.CreateDatabaseAsync($"test_{Guid.NewGuid():N}");

		await using (AppDbContext dbContext = CreateDbContext())
		{
			await dbContext.Database.MigrateAsync(cancellationToken);
		}

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

		builder.ConfigureServices(services =>
		{
			services.AddSingleton<ICurrentUser>(new FixedCurrentUser(TestUser));
			services.AddSingleton<TimeProvider>(this.Clock);
		});
	}
}
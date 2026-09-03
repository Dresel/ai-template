using Argus.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;

namespace Argus.Api.IntegrationTests;

public sealed class ApiFixture(PostgresFixture postgres) : WebApplicationFactory<Program>, IAsyncLifetime
{
	private string connectionString = string.Empty;

	private Respawner respawner = null!;

	public AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(this.connectionString).Options);

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
		builder.UseSetting("ConnectionStrings:argusdb", this.connectionString);
		builder.UseSetting("Database:SeedTestData", bool.FalseString);
	}
}
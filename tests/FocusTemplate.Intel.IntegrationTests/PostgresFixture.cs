using FocusTemplate.Intel.IntegrationTests;
using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(PostgresFixture))]

namespace FocusTemplate.Intel.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
	private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17").Build();
	private readonly SemaphoreSlim createLock = new(1, 1);

	public async Task<string> CreateDatabaseAsync(string name)
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		await this.createLock.WaitAsync(cancellationToken);

		try
		{
			await using NpgsqlConnection admin = new(this.container.GetConnectionString());
			await admin.OpenAsync(cancellationToken);

			using NpgsqlCommandBuilder commandBuilder = new();
			await using NpgsqlCommand command = admin.CreateCommand();
			command.CommandText = $"CREATE DATABASE {commandBuilder.QuoteIdentifier(name)}";

			await command.ExecuteNonQueryAsync(cancellationToken);
		}
		finally
		{
			this.createLock.Release();
		}

		return new NpgsqlConnectionStringBuilder(this.container.GetConnectionString()) { Database = name, }.ConnectionString;
	}

	public async ValueTask DisposeAsync()
	{
		await this.container.DisposeAsync();
		this.createLock.Dispose();
	}

	public async ValueTask InitializeAsync() => await this.container.StartAsync(TestContext.Current.CancellationToken);
}
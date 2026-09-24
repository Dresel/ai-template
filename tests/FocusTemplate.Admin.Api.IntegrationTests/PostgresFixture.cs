using FocusTemplate.Admin.Api.IntegrationTests;
using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(PostgresFixture))]

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
	public const string ReaderRole = "focusdb_reader";

	private const string ReaderPassword = "focusdb_reader";

	private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgis/postgis:17-3.5").Build();
	private readonly SemaphoreSlim createLock = new(1, 1);

	// GRANT ON ALL TABLES reaches only the tables that exist, so this runs after the migrations, not on CREATE DATABASE.
	public static async Task GrantReadAccessAsync(string connectionString)
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		await using NpgsqlConnection connection = new(connectionString);
		await connection.OpenAsync(cancellationToken);

		await using NpgsqlCommand command = connection.CreateCommand();
		command.CommandText = $"""
			GRANT USAGE ON SCHEMA public TO {ReaderRole};
			GRANT SELECT ON ALL TABLES IN SCHEMA public TO {ReaderRole};
			""";

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public static string ReadOnlyConnectionString(string connectionString) =>
		new NpgsqlConnectionStringBuilder(connectionString) { Username = ReaderRole, Password = ReaderPassword, }
			.ConnectionString;

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

		return new NpgsqlConnectionStringBuilder(this.container.GetConnectionString()) { Database = name, }
			.ConnectionString;
	}

	public async ValueTask DisposeAsync()
	{
		await this.container.DisposeAsync();
		this.createLock.Dispose();
	}

	public async ValueTask InitializeAsync()
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		await this.container.StartAsync(cancellationToken);

		await using NpgsqlConnection admin = new(this.container.GetConnectionString());
		await admin.OpenAsync(cancellationToken);

		await using NpgsqlCommand command = admin.CreateCommand();
		command.CommandText = $"CREATE ROLE {ReaderRole} LOGIN PASSWORD '{ReaderPassword}'";

		await command.ExecuteNonQueryAsync(cancellationToken);
	}
}
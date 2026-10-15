using Npgsql;
using Testcontainers.PostgreSql;

namespace SpatialFocus.Testing.Api;

public sealed class PostgresFixture : IAsyncLifetime
{
	public const string ReaderRole = "focusdb_reader";

	private const string ReaderPassword = "focusdb_reader";

	private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgis/postgis:17-3.5").Build();
	private readonly SemaphoreSlim createLock = new(1, 1);

	// GRANT ON ALL TABLES reaches only the tables that exist, so this runs after the migrations, not on CREATE DATABASE.
	// Every schema of the database: the spine's public and one per feature
	public static async Task GrantReadAccessAsync(string connectionString)
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		await using NpgsqlConnection connection = new(connectionString);
		await connection.OpenAsync(cancellationToken);

		await using NpgsqlCommand command = connection.CreateCommand();
		command.CommandText = $"""
			DO $$
			DECLARE schema_name text;
			BEGIN
				FOR schema_name IN
					SELECT nspname FROM pg_namespace WHERE nspname NOT LIKE 'pg\_%' AND nspname <> 'information_schema'
				LOOP
					EXECUTE format('GRANT USAGE ON SCHEMA %I TO {ReaderRole}', schema_name);
					EXECUTE format('GRANT SELECT ON ALL TABLES IN SCHEMA %I TO {ReaderRole}', schema_name);
				END LOOP;
			END $$;
			""";

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public static string ReadOnlyConnectionString(string connectionString) =>
		new NpgsqlConnectionStringBuilder(connectionString) { Username = ReaderRole, Password = ReaderPassword, }.ConnectionString;

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
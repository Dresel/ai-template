namespace FocusTemplate.Names;

// The connection strings the AppHost hands the services, named after the reference
public static class ConnectionNames
{
	public const string Database = "focusdb";

	public const string ReadOnlyDatabase = "focusdb-readonly";

	public const string Redis = "redis";
}
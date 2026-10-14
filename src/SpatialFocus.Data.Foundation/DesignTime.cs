namespace SpatialFocus.Data.Foundation;

public static class DesignTime
{
	public const string ConnectionName = "migrationdb";

	public const string SeedTestDataVariable = "Database__SeedTestData";

	private const string OfflineConnectionString = "Host=localhost";

	public static string ConnectionString =>
		Environment.GetEnvironmentVariable("ConnectionStrings__" + ConnectionName) ?? OfflineConnectionString;

	public static bool SeedTestData =>
		bool.TryParse(Environment.GetEnvironmentVariable(SeedTestDataVariable), out bool seedTestData) && seedTestData;
}
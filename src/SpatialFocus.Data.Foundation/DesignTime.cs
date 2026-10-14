using SpatialFocus.Names;

namespace SpatialFocus.Data.Foundation;

public static class DesignTime
{
	private const string OfflineConnectionString = "Host=localhost";

	public static string ConnectionString =>
		Environment.GetEnvironmentVariable("ConnectionStrings__" + DesignTimeNames.ConnectionName) ?? OfflineConnectionString;

	public static bool SeedTestData =>
		bool.TryParse(Environment.GetEnvironmentVariable(DesignTimeNames.SeedTestDataVariable), out bool seedTestData) && seedTestData;
}
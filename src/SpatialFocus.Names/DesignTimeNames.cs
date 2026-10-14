namespace SpatialFocus.Names;

// What the AppHost hands a migrations resource and the design-time factories read: the database under a name no host
// uses, and whether to seed the development data
public static class DesignTimeNames
{
	public const string ConnectionName = "migrationdb";

	public const string SeedTestDataVariable = "Database__SeedTestData";
}
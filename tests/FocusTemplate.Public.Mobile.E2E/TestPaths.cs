using System.Reflection;

namespace FocusTemplate.Public.Mobile.E2E;

internal static class TestPaths
{
	internal static string RepoRoot { get; } = Assembly.GetExecutingAssembly()
		.GetCustomAttributes<AssemblyMetadataAttribute>()
		.Single(attribute => attribute.Key == "RepoRoot")
		.Value!;

	internal static string ProjectDirectory { get; } =
		Path.Combine(RepoRoot, "tests", "FocusTemplate.Public.Mobile.E2E");
}
using System.Xml.Linq;

namespace SpatialFocus.Testing.Architecture;

// A project as its file declares it, so a new project falls under the rules without being listed. Path is relative to
// the repository root, with forward slashes.
public sealed record ProjectFile(string Name, string Path, IReadOnlyList<string> References)
{
	private static readonly string[] Folders = ["src", "tests",];

	private static readonly string[] TestSuffixes = [".Tests", ".IntegrationTests", ".E2E",];

	// The test libraries under tests/ and the test projects a package keeps beside its own
	public bool IsTest =>
		Path.StartsWith("tests/", StringComparison.Ordinal) || TestSuffixes.Any(suffix => Name.EndsWith(suffix, StringComparison.Ordinal));

	// The projects under src/ and tests/ but the spec projects, which build nothing
	public static IReadOnlyList<ProjectFile> ReadAll(string solutionFile)
	{
		DirectoryInfo root = new(AppContext.BaseDirectory);
		while (!File.Exists(System.IO.Path.Combine(root.FullName, solutionFile)))
		{
			root = root.Parent ?? throw new InvalidOperationException($"No {solutionFile} above the test's directory.");
		}

		return
		[
			.. Folders
				.SelectMany(folder => Directory.EnumerateFiles(
					System.IO.Path.Combine(root.FullName, folder),
					"*.csproj",
					SearchOption.AllDirectories))
				.Where(path => !path.EndsWith(".Spec.csproj", StringComparison.Ordinal))
				.Select(path => new ProjectFile(
					System.IO.Path.GetFileNameWithoutExtension(path),
					System.IO.Path.GetRelativePath(root.FullName, path).Replace('\\', '/'),
					[
						.. XDocument.Load(path)
							.Descendants("ProjectReference")
							.Select(reference =>
								System.IO.Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value.Replace('\\', '/'))),
					])),
		];
	}

	public bool IsIn(string folder) => Path.StartsWith(folder.TrimEnd('/') + "/", StringComparison.Ordinal);
}
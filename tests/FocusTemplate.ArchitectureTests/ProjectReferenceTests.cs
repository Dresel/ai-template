using System.Xml.Linq;

namespace FocusTemplate.ArchitectureTests;

// SpatialFocus.* is what products share, so it references no product, and a shared project without a vertical in its
// name serves every vertical. Read from the project files, so a new project falls under the rules without being listed.
public sealed class ProjectReferenceTests
{
	private static readonly string[] Verticals = ["Admin", "Public",];

	private static readonly Dictionary<string, string[]> References = ReadReferences();

	public static TheoryData<string> SharedProjects =>
		[.. References.Keys.Where(name => name.StartsWith("SpatialFocus.", StringComparison.Ordinal)).Order(StringComparer.Ordinal),];

	public static TheoryData<string> GeneralProjects =>
	[
		.. References.Keys.Where(name => name.StartsWith("SpatialFocus.", StringComparison.Ordinal) && Vertical(name) is null)
			.Order(StringComparer.Ordinal),
	];

	[Theory]
	[MemberData(nameof(SharedProjects))]
	public void ASharedProjectReferencesNoProductProject(string project) =>
		Assert.DoesNotContain(References[project], reference => reference.StartsWith("FocusTemplate.", StringComparison.Ordinal));

	[Theory]
	[MemberData(nameof(GeneralProjects))]
	public void AGeneralProjectReferencesNoVertical(string project) =>
		Assert.DoesNotContain(References[project], reference => Vertical(reference) is not null);

	// SpatialFocus.Admin.UserManagement.Client and FocusTemplate.Public.Api name a vertical, SpatialFocus.Web.Hosting none
	private static string? Vertical(string project) =>
		project.Split('.') is [_, var second, ..,] && Verticals.Contains(second, StringComparer.Ordinal) ? second : null;

	private static Dictionary<string, string[]> ReadReferences()
	{
		DirectoryInfo root = new(AppContext.BaseDirectory);
		while (!File.Exists(Path.Combine(root.FullName, "FocusTemplate.slnx")))
		{
			root = root.Parent ?? throw new InvalidOperationException("No FocusTemplate.slnx above the test's directory.");
		}

		return Directory.EnumerateFiles(Path.Combine(root.FullName, "src"), "*.csproj", SearchOption.AllDirectories)
			.Where(path => !path.EndsWith(".Spec.csproj", StringComparison.Ordinal))
			.ToDictionary(
				path => Path.GetFileNameWithoutExtension(path),
				path => XDocument.Load(path)
					.Descendants("ProjectReference")
					.Select(reference => Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value.Replace('\\', '/')))
					.ToArray());
	}
}
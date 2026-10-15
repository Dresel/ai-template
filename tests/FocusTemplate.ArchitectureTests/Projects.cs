using SpatialFocus.Testing.Architecture;

namespace FocusTemplate.ArchitectureTests;

internal static class Projects
{
	public static IReadOnlyList<ProjectFile> All { get; } = ProjectFile.ReadAll("FocusTemplate.slnx");

	public static ProjectFile Named(string name) => All.Single(project => project.Name == name);
}
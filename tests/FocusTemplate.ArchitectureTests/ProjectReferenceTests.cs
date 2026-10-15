using SpatialFocus.Testing.Architecture;

namespace FocusTemplate.ArchitectureTests;

public sealed class ProjectReferenceTests
{
	private static readonly string[] Verticals = ["Admin", "Public",];

	public static TheoryData<string> SharedProjects =>
	[
		.. Projects.All.Where(project => project.Name.StartsWith(ProjectReferenceRules.SharedPrefix, StringComparison.Ordinal))
			.Select(project => project.Name)
			.Order(StringComparer.Ordinal),
	];

	public static TheoryData<string> GeneralProjects =>
	[
		.. Projects.All.Where(project => project.Name.StartsWith(ProjectReferenceRules.SharedPrefix, StringComparison.Ordinal) &&
				!project.IsTest && ProjectReferenceRules.Vertical(project.Name, Verticals) is null)
			.Select(project => project.Name)
			.Order(StringComparer.Ordinal),
	];

	[Theory]
	[MemberData(nameof(SharedProjects))]
	public void ASharedProjectReferencesNoProductProject(string project) =>
		Assert.Empty(ProjectReferenceRules.ProductReferences(Projects.Named(project), "FocusTemplate."));

	[Theory]
	[MemberData(nameof(GeneralProjects))]
	public void AGeneralProjectReferencesNoVertical(string project) =>
		Assert.Empty(ProjectReferenceRules.VerticalReferences(Projects.Named(project), Verticals));
}
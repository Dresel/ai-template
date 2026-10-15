namespace SpatialFocus.Testing.Architecture;

// SpatialFocus.* is what products share, so it references no product, and a shared project without a vertical in its
// name serves every vertical. A test project composes what it tests, so the vertical rule holds for production alone.
public static class ProjectReferenceRules
{
	public const string SharedPrefix = "SpatialFocus.";

	public static IEnumerable<string> ProductReferences(ProjectFile project, string productPrefix) =>
		project.References.Where(reference => reference.StartsWith(productPrefix, StringComparison.Ordinal));

	public static IEnumerable<string> VerticalReferences(ProjectFile project, IReadOnlyCollection<string> verticals) =>
		project.References.Where(reference => Vertical(reference, verticals) is not null);

	// SpatialFocus.Admin.UserManagement.Client and FocusTemplate.Public.Api name a vertical, SpatialFocus.Web.Hosting none
	public static string? Vertical(string project, IReadOnlyCollection<string> verticals) =>
		project.Split('.') is [_, var second, ..,] && verticals.Contains(second, StringComparer.Ordinal) ? second : null;
}
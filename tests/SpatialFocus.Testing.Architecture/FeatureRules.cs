using System.Reflection;

namespace SpatialFocus.Testing.Architecture;

// A feature drops into another host only while it builds on the spine alone
public static class FeatureRules
{
	// An allowlist, so a new shared project stays closed to the features until it is added here
	public static IReadOnlyList<string> Foundations { get; } =
	[
		"SpatialFocus.Api.Foundation",
		"SpatialFocus.Data.Foundation",
		"SpatialFocus.Http",
		"SpatialFocus.Primitives",
		"SpatialFocus.Web.ClientServiceDefaults",
		"SpatialFocus.Web.Foundation",
	];

	// A feature's tests stand in for a host: the test libraries and the localization a host registers come on top
	public static IReadOnlyList<string> TestSupport { get; } =
	[
		"SpatialFocus.Localization",
		"SpatialFocus.Testing",
		"SpatialFocus.Testing.Host",
	];

	// What a feature's assembly references of the repository's own beyond its own projects and the foundations
	public static IEnumerable<string> ForeignReferences(Assembly inTheFeature, IReadOnlyCollection<string> ownPrefixes)
	{
		string feature = FeatureOf(inTheFeature.GetName().Name!);

		return inTheFeature.GetReferencedAssemblies()
			.Select(reference => reference.Name!)
			.Where(name => ownPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
			.Where(name => !name.StartsWith(feature, StringComparison.Ordinal) && !Foundations.Contains(name, StringComparer.Ordinal))
			.Order(StringComparer.Ordinal);
	}

	// The same from a project file in a feature's folder, which also holds the feature's test projects
	public static IEnumerable<string> ForeignReferences(ProjectFile inTheFeature)
	{
		string feature = FeatureOf(inTheFeature.Name);
		IEnumerable<string> allowed = inTheFeature.IsTest ? [.. Foundations, .. TestSupport,] : Foundations;

		return inTheFeature.References
			.Where(name => !name.StartsWith(feature, StringComparison.Ordinal) && !allowed.Contains(name, StringComparer.Ordinal))
			.Order(StringComparer.Ordinal);
	}

	// SpatialFocus.Weather.Api belongs to the feature SpatialFocus.Weather
	private static string FeatureOf(string project) => string.Join('.', project.Split('.')[..2]) + ".";
}
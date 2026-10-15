using SpatialFocus.Data;
using SpatialFocus.Testing.Architecture;
using SpatialFocus.Weather.Api;
using SpatialFocus.Weather.Client.Stations;
using SpatialFocus.Weather.Data;
using SpatialFocus.Weather.Shared;
using ServiceCollectionExtensions = SpatialFocus.Weather.Web.ServiceCollectionExtensions;

namespace FocusTemplate.ArchitectureTests;

public sealed class FeatureTests
{
	// The repository's own assemblies, the hosts' and the shared ones
	private static readonly string[] OwnPrefixes = ["FocusTemplate.", "SpatialFocus.",];

	public static TheoryData<Type> FeatureAssemblies =>
	[
		typeof(WeatherEndpoints),
		typeof(StationsClient),
		typeof(WeatherDbContext),
		typeof(StationResponse),
		typeof(ServiceCollectionExtensions),
	];

	// Every project in a feature's folder, the feature's test projects included
	public static TheoryData<string> FeatureProjects =>
	[
		.. Projects.All.Where(project => project.IsIn("src/features")).Select(project => project.Name).Order(StringComparer.Ordinal),
	];

	public static TheoryData<Type> PooledContexts => [.. DbContextRules.PooledContexts(typeof(AppDbContext), typeof(WeatherDbContext)),];

	[Theory]
	[MemberData(nameof(FeatureAssemblies))]
	public void AFeatureReferencesItsOwnProjectsAndTheFoundationsAlone(Type inTheFeature) =>
		Assert.Empty(FeatureRules.ForeignReferences(inTheFeature.Assembly, OwnPrefixes));

	[Theory]
	[MemberData(nameof(FeatureProjects))]
	public void AFeatureProjectReferencesItsOwnProjectsAndTheFoundationsAlone(string project) =>
		Assert.Empty(FeatureRules.ForeignReferences(Projects.Named(project)));

	[Theory]
	[MemberData(nameof(PooledContexts))]
	public void APooledContextTakesItsOptionsAlone(Type context) => Assert.True(DbContextRules.TakesItsOptionsAlone(context));
}
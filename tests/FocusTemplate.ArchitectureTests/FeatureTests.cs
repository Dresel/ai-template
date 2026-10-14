using Microsoft.EntityFrameworkCore;
using SpatialFocus.Data;
using SpatialFocus.Weather.Api;
using SpatialFocus.Weather.Client.Stations;
using SpatialFocus.Weather.Data;
using SpatialFocus.Weather.Shared;
using ServiceCollectionExtensions = SpatialFocus.Weather.Web.ServiceCollectionExtensions;

namespace FocusTemplate.ArchitectureTests;

// A feature drops into another host only while it builds on the spine alone
public sealed class FeatureTests
{
	// An allowlist, so a new shared project stays closed to the features until it is added here
	private static readonly string[] Foundations =
	[
		"SpatialFocus.Api.Foundation",
		"SpatialFocus.Data.Foundation",
		"SpatialFocus.Http",
		"SpatialFocus.Primitives",
		"SpatialFocus.Web.ClientServiceDefaults",
		"SpatialFocus.Web.Foundation",
	];

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

	// A context pool hands one instance to request after request, so a constructor may capture nothing that differs per
	// request. The auditing interceptor reads the user on each save.
	public static TheoryData<Type> PooledContexts =>
	[
		.. new[] { typeof(AppDbContext), typeof(WeatherDbContext), }.SelectMany(anchor => anchor.Assembly.GetTypes())
			.Where(type => typeof(DbContext).IsAssignableFrom(type) && !type.IsAbstract),
	];

	[Theory]
	[MemberData(nameof(FeatureAssemblies))]
	public void AFeatureReferencesItsOwnProjectsAndTheFoundationsAlone(Type inTheFeature)
	{
		// SpatialFocus.Weather.Api belongs to the feature SpatialFocus.Weather
		string feature = string.Join('.', inTheFeature.Assembly.GetName().Name!.Split('.')[..2]) + ".";

		Assert.DoesNotContain(
			inTheFeature.Assembly.GetReferencedAssemblies()
				.Select(reference => reference.Name!)
				.Where(name => OwnPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))),
			name => !name.StartsWith(feature, StringComparison.Ordinal) && !Foundations.Contains(name, StringComparer.Ordinal));
	}

	[Theory]
	[MemberData(nameof(PooledContexts))]
	public void APooledContextTakesItsOptionsAlone(Type context) =>
		Assert.Equal(
			[typeof(DbContextOptions<>).MakeGenericType(context),],
			context.GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType));
}
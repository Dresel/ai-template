using FocusTemplate.Data;
using FocusTemplate.Weather.Api;
using FocusTemplate.Weather.Client.Stations;
using FocusTemplate.Weather.Data;
using FocusTemplate.Weather.Shared;
using Microsoft.EntityFrameworkCore;
using ServiceCollectionExtensions = FocusTemplate.Weather.Web.ServiceCollectionExtensions;

namespace FocusTemplate.ArchitectureTests;

// A feature drops into another host only while it builds on the spine alone
public sealed class FeatureTests
{
	// An allowlist, so a new shared project stays closed to the features until it is added here
	private static readonly string[] Foundations =
	[
		"FocusTemplate.Admin.Web.ClientServiceDefaults",
		"FocusTemplate.Admin.Web.Foundation",
		"FocusTemplate.Api.Foundation",
		"FocusTemplate.Data.Foundation",
		"FocusTemplate.Http",
		"FocusTemplate.Primitives",
	];

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
		// FocusTemplate.Weather.Api belongs to the feature FocusTemplate.Weather
		string feature = string.Join('.', inTheFeature.Assembly.GetName().Name!.Split('.')[..2]) + ".";

		Assert.DoesNotContain(
			inTheFeature.Assembly.GetReferencedAssemblies()
				.Select(reference => reference.Name!)
				.Where(name => name.StartsWith("FocusTemplate.", StringComparison.Ordinal)),
			name => !name.StartsWith(feature, StringComparison.Ordinal) && !Foundations.Contains(name, StringComparer.Ordinal));
	}

	[Theory]
	[MemberData(nameof(PooledContexts))]
	public void APooledContextTakesItsOptionsAlone(Type context) =>
		Assert.Equal(
			[typeof(DbContextOptions<>).MakeGenericType(context),],
			context.GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType));
}
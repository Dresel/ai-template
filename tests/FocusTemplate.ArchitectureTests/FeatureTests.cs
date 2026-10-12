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
	private static readonly string[] HostAssemblies =
	[
		"FocusTemplate.Admin.Api",
		"FocusTemplate.Admin.Client",
		"FocusTemplate.Admin.Shared",
		"FocusTemplate.Admin.Web",
		"FocusTemplate.Data",
		"FocusTemplate.Public.Api",
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
	public void AFeatureReferencesNoHostProject(Type inTheFeature) =>
		Assert.DoesNotContain(
			inTheFeature.Assembly.GetReferencedAssemblies().Select(reference => reference.Name),
			name => HostAssemblies.Contains(name, StringComparer.Ordinal));

	[Theory]
	[MemberData(nameof(PooledContexts))]
	public void APooledContextTakesItsOptionsAlone(Type context) =>
		Assert.Equal(
			[typeof(DbContextOptions<>).MakeGenericType(context),],
			context.GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType));
}
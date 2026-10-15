using FocusTemplate.Admin.Api.Features.DemoProfiles;
using FocusTemplate.Public.Api.Features.WeatherForecasts;
using SpatialFocus.Admin.UserManagement.Api.Groups;
using SpatialFocus.Testing.Architecture;
using SpatialFocus.Weather.Api.Stations;

namespace FocusTemplate.ArchitectureTests;

// Every handler of the APIs and the features
public sealed class HandlerDbContextTests
{
	// Anchored on a handler rather than Program: both APIs emit a Program class in the global namespace.
	public static TheoryData<Type> Apis =>
	[
		typeof(DemoProfilesCreateHandler), typeof(GroupsGetHandler), typeof(WeatherForecastsListHandler), typeof(StationsListHandler),
	];

	[Theory]
	[MemberData(nameof(Apis))]
	public void CommandHandlersTakeAWritableContext(Type handlerInTheApi) =>
		Assert.Empty(HandlerRules.CommandHandlersTakingNoWritableContext(handlerInTheApi.Assembly));

	[Theory]
	[MemberData(nameof(Apis))]
	public void QueryHandlersTakeAReadOnlyContext(Type handlerInTheApi) =>
		Assert.Empty(HandlerRules.QueryHandlersTakingAWritableContext(handlerInTheApi.Assembly));
}
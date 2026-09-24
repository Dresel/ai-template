using System.Reflection;
using FocusTemplate.Admin.Api.Features.WeatherForecasts;
using FocusTemplate.Data;
using Mediator;

namespace FocusTemplate.ArchitectureTests;

// Queries read through ReadOnlyAppDbContext and commands write through AppDbContext. The SELECT-only role behind the
// read-only connection guarantees nothing for a query handler that holds the write context, so the convention is
// checked here for every handler of both APIs, tested or not.
public sealed class HandlerDbContextTests
{
	private static readonly Type[] CommandHandlerInterfaces = [typeof(ICommandHandler<,>), typeof(ICommandHandler<>),];

	private static readonly Type[] ForbiddenForCommands = [typeof(ReadOnlyAppDbContext), typeof(AppDbContextBase),];

	private static readonly Type[] ForbiddenForQueries = [typeof(AppDbContext), typeof(AppDbContextBase),];
	private static readonly Type[] QueryHandlerInterfaces = [typeof(IQueryHandler<,>),];

	[Theory]
	[InlineData(typeof(WeatherForecastsListHandler))]
	[InlineData(typeof(Public.Api.Features.WeatherForecasts.WeatherForecastsListHandler))]
	public void CommandHandlersTakeTheWriteContext(Type handlerInTheApi) =>
		Assert.Empty(HandlersDependingOn(handlerInTheApi.Assembly, CommandHandlerInterfaces, ForbiddenForCommands));

	// Anchored on a handler rather than Program: both APIs emit a Program class in the global namespace.
	[Theory]
	[InlineData(typeof(WeatherForecastsListHandler))]
	[InlineData(typeof(Public.Api.Features.WeatherForecasts.WeatherForecastsListHandler))]
	public void QueryHandlersTakeTheReadOnlyContext(Type handlerInTheApi) =>
		Assert.Empty(HandlersDependingOn(handlerInTheApi.Assembly, QueryHandlerInterfaces, ForbiddenForQueries));

	private static IEnumerable<string>
		HandlersDependingOn(Assembly api, Type[] handlerInterfaces, Type[] forbiddenParameterTypes) =>
		api.GetTypes()
			.Where(type => type.GetInterfaces()
				.Any(implemented => implemented.IsGenericType &&
					handlerInterfaces.Contains(implemented.GetGenericTypeDefinition())))
			.SelectMany(type => type.GetConstructors()
				.SelectMany(constructor => constructor.GetParameters())
				.Where(parameter => forbiddenParameterTypes.Contains(parameter.ParameterType))
				.Select(parameter => $"{type.Name} takes {parameter.ParameterType.Name}"))
			.Order(StringComparer.Ordinal);
}
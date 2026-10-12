using System.Reflection;
using FocusTemplate.Admin.Api.Features.UserManagement.Groups;
using FocusTemplate.Data.Foundation;
using FocusTemplate.Public.Api.Features.WeatherForecasts;
using FocusTemplate.Weather.Api.Stations;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.ArchitectureTests;

// The SELECT-only role protects a read-only context alone, so every handler of the APIs and the features is checked for
// its side: a read-only context for a query, a writable one for a command, and an abstract base for neither.
public sealed class HandlerDbContextTests
{
	private static readonly Type[] CommandHandlerInterfaces = [typeof(ICommandHandler<,>), typeof(ICommandHandler<>),];

	private static readonly Type[] QueryHandlerInterfaces = [typeof(IQueryHandler<,>),];

	// Anchored on a handler rather than Program: both APIs emit a Program class in the global namespace.
	public static TheoryData<Type> Apis => [typeof(GroupsGetHandler), typeof(WeatherForecastsListHandler), typeof(StationsListHandler),];

	[Theory]
	[MemberData(nameof(Apis))]
	public void CommandHandlersTakeAWritableContext(Type handlerInTheApi) =>
		Assert.Empty(
			ContextsTaken(handlerInTheApi.Assembly, CommandHandlerInterfaces, context => context.IsAbstract || IsReadOnly(context)));

	[Theory]
	[MemberData(nameof(Apis))]
	public void QueryHandlersTakeAReadOnlyContext(Type handlerInTheApi) =>
		Assert.Empty(ContextsTaken(handlerInTheApi.Assembly, QueryHandlerInterfaces, context => !IsReadOnly(context)));

	private static IEnumerable<string> ContextsTaken(Assembly api, Type[] handlerInterfaces, Func<Type, bool> forbidden) =>
		api.GetTypes()
			.Where(type => type.GetInterfaces()
				.Any(implemented => implemented.IsGenericType && handlerInterfaces.Contains(implemented.GetGenericTypeDefinition())))
			.SelectMany(type => type.GetConstructors()
				.SelectMany(constructor => constructor.GetParameters())
				.Where(parameter => typeof(DbContext).IsAssignableFrom(parameter.ParameterType) && forbidden(parameter.ParameterType))
				.Select(parameter => $"{type.Name} takes {parameter.ParameterType.Name}"))
			.Order(StringComparer.Ordinal);

	private static bool IsReadOnly(Type context) => typeof(IReadOnlyDbContext).IsAssignableFrom(context);
}
using System.Reflection;
using Mediator;
using Microsoft.EntityFrameworkCore;
using SpatialFocus.Data.Foundation;

namespace SpatialFocus.Testing.Architecture;

// The SELECT-only role protects a read-only context alone, so every handler is checked for its side: a read-only
// context for a query, a writable one for a command, and an abstract base for neither.
public static class HandlerRules
{
	private static readonly Type[] CommandHandlerInterfaces = [typeof(ICommandHandler<,>), typeof(ICommandHandler<>),];

	private static readonly Type[] QueryHandlerInterfaces = [typeof(IQueryHandler<,>),];

	public static IEnumerable<string> CommandHandlersTakingNoWritableContext(Assembly api) =>
		ContextsTaken(api, CommandHandlerInterfaces, context => context.IsAbstract || IsReadOnly(context));

	public static IEnumerable<string> QueryHandlersTakingAWritableContext(Assembly api) =>
		ContextsTaken(api, QueryHandlerInterfaces, context => !IsReadOnly(context));

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
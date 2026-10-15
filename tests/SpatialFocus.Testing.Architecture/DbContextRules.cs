using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SpatialFocus.Data.Entities;
using SpatialFocus.Data.Foundation.Identity;

namespace SpatialFocus.Testing.Architecture;

public static class DbContextRules
{
	// A context pool hands one instance to request after request, so a constructor may capture nothing that differs per
	// request. The auditing interceptor reads the user on each save.
	public static bool TakesItsOptionsAlone(Type context) =>
		context.GetConstructors() is [var constructor,] && constructor.GetParameters() is [var options,] &&
		options.ParameterType == typeof(DbContextOptions<>).MakeGenericType(context);

	public static IEnumerable<Type> PooledContexts(params Type[] inTheAssemblies) =>
		inTheAssemblies.SelectMany(anchor => anchor.Assembly.GetTypes())
			.Where(type => typeof(DbContext).IsAssignableFrom(type) && !type.IsAbstract);

	// Converts each sentinel as dbcontext optimize does, so a typed id without one fails here rather than at publish
	public static IEnumerable<string> UnconvertibleSentinels(DbContext context) =>
		context.Model.GetEntityTypes()
			.SelectMany(entityType => entityType.GetProperties())
			.Where(property => !Converts(property))
			.Select(property => $"{property.DeclaringType.DisplayName()}.{property.Name}")
			.Order(StringComparer.Ordinal);

	// A feature reads the spine's users through UserReference, which no migration of the feature creates. A column the
	// spine renames or drops breaks the feature's queries at run time, so the two mappings are compared.
	public static IEnumerable<string> UserReferenceMismatches(DbContext spine, DbContext feature)
	{
		IEntityType user = spine.Model.FindEntityType(typeof(User))!;
		IEntityType reference = feature.Model.FindEntityType(typeof(UserReference))!;

		if ((user.GetTableName(), user.GetSchema() ?? "public") != (reference.GetTableName(), reference.GetSchema()))
		{
			yield return $"UserReference maps {reference.GetSchema()}.{reference.GetTableName()}";
		}

		foreach (IProperty property in reference.GetProperties())
		{
			IProperty? column = user.FindProperty(property.Name);
			if (column is null || (column.GetColumnName(), column.ClrType, column.IsNullable) !=
				(property.GetColumnName(), property.ClrType, property.IsNullable))
			{
				yield return $"UserReference.{property.Name} maps no column of the users";
			}
		}
	}

	private static bool Converts(IProperty property)
	{
		try
		{
			property.GetTypeMapping().Converter?.ConvertToProvider(property.Sentinel);

			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
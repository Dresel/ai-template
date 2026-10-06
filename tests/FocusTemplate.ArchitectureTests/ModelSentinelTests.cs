using FocusTemplate.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FocusTemplate.ArchitectureTests;

// Converts each sentinel as dbcontext optimize does, so a typed id without one fails here rather than at publish
public sealed class ModelSentinelTests
{
	[Fact]
	public void EverySentinelConvertsToItsProviderValue()
	{
		using ReadOnlyAppDbContext context = new(
			new DbContextOptionsBuilder<ReadOnlyAppDbContext>().ConfigureReadOnlyAppDbContext("Host=localhost").Options);

		IEnumerable<string> refused = context.Model.GetEntityTypes()
			.SelectMany(entityType => entityType.GetProperties())
			.Where(property => !Converts(property))
			.Select(property => $"{property.DeclaringType.DisplayName()}.{property.Name}")
			.Order(StringComparer.Ordinal);

		Assert.Empty(refused);
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
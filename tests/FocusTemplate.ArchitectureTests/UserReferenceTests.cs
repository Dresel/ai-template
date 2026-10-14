using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SpatialFocus.Data.Entities;
using SpatialFocus.Data.Foundation.Identity;

namespace FocusTemplate.ArchitectureTests;

// A feature reads the spine's users through UserReference, which no migration of the feature creates. A column the spine
// renames or drops breaks the feature's queries at run time, so the two mappings are compared here.
public sealed class UserReferenceTests
{
	[Fact]
	public void UserReferenceMapsColumnsOfTheSpinesUsers()
	{
		using DbContext spine = Contexts.ReadOnly("spine");
		using DbContext feature = Contexts.ReadOnly("weather");
		IEntityType user = spine.Model.FindEntityType(typeof(User))!;
		IEntityType reference = feature.Model.FindEntityType(typeof(UserReference))!;

		Assert.Equal((user.GetTableName(), user.GetSchema() ?? "public"), (reference.GetTableName(), reference.GetSchema()));
		Assert.All(
			reference.GetProperties(),
			property =>
			{
				IProperty? column = user.FindProperty(property.Name);
				Assert.NotNull(column);
				Assert.Equal(
					(column.GetColumnName(), column.ClrType, column.IsNullable),
					(property.GetColumnName(), property.ClrType, property.IsNullable));
			});
	}
}
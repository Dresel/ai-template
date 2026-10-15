using Microsoft.EntityFrameworkCore;
using SpatialFocus.Testing.Architecture;

namespace FocusTemplate.ArchitectureTests;

public sealed class UserReferenceTests
{
	[Fact]
	public void UserReferenceMapsColumnsOfTheSpinesUsers()
	{
		using DbContext spine = Contexts.ReadOnly("spine");
		using DbContext feature = Contexts.ReadOnly("weather");

		Assert.Empty(DbContextRules.UserReferenceMismatches(spine, feature));
	}
}
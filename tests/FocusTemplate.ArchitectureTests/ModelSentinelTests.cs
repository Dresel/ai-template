using Microsoft.EntityFrameworkCore;
using SpatialFocus.Testing.Architecture;

namespace FocusTemplate.ArchitectureTests;

public sealed class ModelSentinelTests
{
	public static TheoryData<string> Models => ["spine", "weather",];

	[Theory]
	[MemberData(nameof(Models))]
	public void EverySentinelConvertsToItsProviderValue(string model)
	{
		using DbContext context = Contexts.ReadOnly(model);

		Assert.Empty(DbContextRules.UnconvertibleSentinels(context));
	}
}
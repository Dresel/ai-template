#pragma warning disable CA1711
namespace FocusTemplate.Public.Mobile.E2E;

[CollectionDefinition(Name)]
public sealed class AppiumCollection : ICollectionFixture<AppiumFixture>
{
	public const string Name = "appium";
}
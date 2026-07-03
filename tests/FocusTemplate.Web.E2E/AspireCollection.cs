#pragma warning disable CA1711
namespace FocusTemplate.Web.E2E;

[CollectionDefinition(Name)]
public sealed class AspireCollection : ICollectionFixture<BlazorAppFixture>
{
	public const string Name = "aspire";
}
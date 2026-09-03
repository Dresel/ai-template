#pragma warning disable CA1711
namespace Argus.Web.E2E;

[CollectionDefinition(Name)]
public sealed class AspireCollection : ICollectionFixture<BlazorAppFixture>
{
	public const string Name = "aspire";
}
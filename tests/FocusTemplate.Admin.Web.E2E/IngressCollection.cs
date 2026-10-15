#pragma warning disable CA1711
namespace FocusTemplate.Admin.Web.E2E;

// Not parallel: xunit runs it after the other collections, so its AppHost never boots next to AdminAppFixture's.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IngressCollection : ICollectionFixture<AdminIngressAppFixture>
{
	public const string Name = "ingress";
}
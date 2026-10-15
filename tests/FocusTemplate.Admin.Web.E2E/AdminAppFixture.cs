using FocusTemplate.Names;
using Projects;

namespace FocusTemplate.Admin.Web.E2E;

// The realm is the one keycloak/focus-realm.json imports
public sealed class AdminAppFixture() : BlazorAppFixture(ResourceNames.AdminBff, ResourceNames.Keycloak, "focus")
{
	protected override Task<IDistributedApplicationTestingBuilder> CreateBuilderAsync(CancellationToken cancellationToken) =>
		DistributedApplicationTestingBuilder.CreateAsync<FocusTemplate_AppHost>(
			[
				"Features:TlsOffloadingIngress=false",
				"Features:Analytics=false",
				"Features:Mobile=false",
				"Features:LocalKeycloak=true",
				"Features:PersistentLocalKeycloak=false",
				"Features:PersistentDatabase=false",
				"Features:Chaos=false",
			],
			cancellationToken);
}
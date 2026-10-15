using Projects;

namespace FocusTemplate.Admin.Web.E2E;

public sealed class AdminIngressAppFixture() : IngressAppFixture("admin-bff-ingress")
{
	protected override Task<IDistributedApplicationTestingBuilder> CreateBuilderAsync(CancellationToken cancellationToken) =>
		DistributedApplicationTestingBuilder.CreateAsync<FocusTemplate_AppHost>(
			[
				"Features:TlsOffloadingIngress=true",
				"Features:Analytics=false",
				"Features:Mobile=false",
				"Features:LocalKeycloak=true",
				"Features:PersistentLocalKeycloak=false",
				"Features:PersistentDatabase=false",
				"Features:Chaos=false",
			],
			cancellationToken);
}
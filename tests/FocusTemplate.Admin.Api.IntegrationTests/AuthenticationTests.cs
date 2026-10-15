using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class AuthenticationTests(ApiFixture factory) : ApiTestBase(factory)
{
	// Health probes and the API reference, its documents and the Scalar page with its assets, are the only endpoints a
	// caller reaches without a token.
	private static readonly string[] AnonymousRoutes =
	[
		"/health",
		"/alive",
		"/openapi/{name}.yaml",
		"/scalar/{documentName?}",
		"/scalar/scalar.js",
		"/scalar/scalar.aspnetcore.js",
		"/scalar/favicon.svg",
	];

	// A fallback policy would also lock the health endpoints Aspire probes, so the generated endpoints carry the
	// authorization of the spec's @useAuth, and this test is what catches a spec that lost it: the product's and every
	// package's the host maps.
	[Fact]
	public void EveryApiEndpointRequiresAuthorization()
	{
		IEnumerable<string> unprotected = Factory.Services.GetRequiredService<EndpointDataSource>()
			.Endpoints.OfType<RouteEndpoint>()
			.Where(endpoint => !AnonymousRoutes.Contains(endpoint.RoutePattern.RawText, StringComparer.Ordinal))
			.Where(endpoint => endpoint.Metadata.GetMetadata<IAuthorizeData>() is null ||
				endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
			.Select(endpoint => endpoint.DisplayName ?? endpoint.RoutePattern.RawText ?? "<unnamed>")
			.Order(StringComparer.Ordinal);

		Assert.Empty(unprotected);
	}
}
using System.Net;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class AuthenticationTests(ApiFixture factory) : ApiTestBase(factory)
{
	// Health probes and the OpenAPI document are the only endpoints a caller reaches without a token.
	private static readonly string[] AnonymousRoutes = ["/health", "/alive", "/openapi/v1.yaml",];

	[Fact]
	public async Task AnonymousRequestsAreRejectedWith401()
	{
		using HttpClient client = Factory.CreateClient();

		using HttpResponseMessage response = await client.GetAsync(
			"/weather-forecasts",
			TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	// A fallback policy would also lock the health endpoints Aspire probes, so authorization is attached per endpoint
	// group in the slice's hooks file, and this test is what catches a new slice that forgot its hook.
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

	// Jobs and the seed have no request, and their writes must not be attributed to a person.
	[Fact]
	public void OutsideARequestTheActingUserIsSystem()
	{
		using IServiceScope scope = Factory.Services.CreateScope();
		scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = null;

		Assert.Equal(WellKnownUsers.System, scope.ServiceProvider.GetRequiredService<ICurrentUser>().Id);
	}

	[Fact]
	public void TheActingUserIsTheTokenSubject()
	{
		UserId subject = UserId.From(new Guid("00000000-0000-7000-8000-00000000005b"));

		using IServiceScope scope = Factory.Services.CreateScope();
		scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
		{
			User = TestAuthenticationHandler.CreatePrincipal(subject), RequestServices = scope.ServiceProvider,
		};

		Assert.Equal(subject, scope.ServiceProvider.GetRequiredService<ICurrentUser>().Id);
	}
}
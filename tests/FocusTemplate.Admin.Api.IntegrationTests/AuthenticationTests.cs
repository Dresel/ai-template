using System.Net;
using System.Security.Claims;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class AuthenticationTests(ApiFixture factory) : ApiTestBase(factory)
{
	// Health probes and the OpenAPI document are the only endpoints a caller reaches without a token.
	private static readonly string[] AnonymousRoutes = ["/health", "/alive", "/openapi/v1.yaml",];

	// The test scheme is the default, so the JwtBearer check is driven through its event directly.
	[Fact]
	public async Task ATokenWhoseSubjectIsNoUuidIsRejected()
	{
		JwtBearerOptions options = Factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
			.Get(JwtBearerDefaults.AuthenticationScheme);
		TokenValidatedContext context = new(
			new DefaultHttpContext { RequestServices = Factory.Services, },
			new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
			options)
		{
			Principal = new ClaimsPrincipal(
				new ClaimsIdentity(
					[new Claim(JwtRegisteredClaimNames.Sub, "service-account-admin-bff"),],
					JwtBearerDefaults.AuthenticationScheme)),
		};

		await options.Events.TokenValidated(context);

		Assert.NotNull(context.Result?.Failure);
	}

	[Fact]
	public async Task AnonymousRequestsAreRejectedWith401()
	{
		using HttpClient client = Factory.CreateClient();

		using HttpResponseMessage response = await client.GetAsync(
			"/weather-forecasts",
			TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	// A fallback policy would also lock the health endpoints Aspire probes, so the generated endpoints carry the
	// authorization of the spec's @useAuth, and this test is what catches a spec that lost it.
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

	// Jobs have no request, and nobody may stand in for the person who is not there.
	[Fact]
	public void OutsideARequestNobodyIsSignedIn()
	{
		using IServiceScope scope = Factory.Services.CreateScope();
		scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = null;
		ICurrentUser currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

		Assert.Null(currentUser.IdOrDefault);
		Assert.Throws<InvalidOperationException>(() => currentUser.Id);
	}

	[Fact]
	public void TheSignedInUserIsTheTokenSubject()
	{
		UserId subject = UserId.From(new Guid("00000000-0000-7000-8000-00000000005b"));

		using IServiceScope scope = Factory.Services.CreateScope();
		scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
		{
			User = TestAuthenticationHandler.CreatePrincipal(subject), RequestServices = scope.ServiceProvider,
		};
		ICurrentUser currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

		Assert.Equal(subject, currentUser.IdOrDefault);
		Assert.Equal(subject, currentUser.Id);
	}
}
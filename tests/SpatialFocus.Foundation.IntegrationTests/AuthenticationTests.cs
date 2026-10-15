using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using SpatialFocus.Data.Foundation.Auditing;
using SpatialFocus.Primitives;

namespace SpatialFocus.Foundation.IntegrationTests;

// The Keycloak bearer of Api.Hosting and its current user, as every API host registers them
public sealed class AuthenticationTests(ApiFixture factory) : ApiTestBase(factory)
{
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

		using HttpResponseMessage response = await client.GetAsync("/groups", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
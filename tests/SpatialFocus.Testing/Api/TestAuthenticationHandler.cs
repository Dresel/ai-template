using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using SpatialFocus.Primitives;

namespace SpatialFocus.Testing.Api;

// "Authorization: Test <UserId>" authenticates users, all other requests are anonymous.
// Claims mirror Keycloak access tokens, so existing API mapping works unchanged.
public sealed class TestAuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "Test";

	public static UserId TestUser { get; } = UserId.From(new Guid("00000000-0000-7000-8000-00000000c0de"));

	public static HttpClient Authenticate(HttpClient client, UserId? user = null)
	{
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(SchemeName, (user ?? TestUser).Value.ToString());

		return client;
	}

	public static ClaimsPrincipal CreatePrincipal(UserId user) =>
		new(
			new ClaimsIdentity(
				[
					new Claim(JwtRegisteredClaimNames.Sub, user.Value.ToString()),
					new Claim(JwtRegisteredClaimNames.PreferredUsername, $"user-{user.Value:N}"),
				],
				SchemeName,
				JwtRegisteredClaimNames.PreferredUsername,
				ClaimTypes.Role));

	protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
		Task.FromResult(
			AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out AuthenticationHeaderValue? header) &&
			header.Scheme == SchemeName && Guid.TryParse(header.Parameter, out Guid id)
				? AuthenticateResult.Success(new AuthenticationTicket(CreatePrincipal(UserId.From(id)), SchemeName))
				: AuthenticateResult.NoResult());
}
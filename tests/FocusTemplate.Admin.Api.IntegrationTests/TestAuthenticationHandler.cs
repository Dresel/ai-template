using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FocusTemplate.Primitives;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FocusTemplate.Admin.Api.IntegrationTests;

// "Authorization: Test <UserId>" authenticates users; all other requests are anonymous.
// Claims mirror Keycloak access tokens, so existing API mapping works unchanged.
public sealed class TestAuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "Test";

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
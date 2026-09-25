using System.Text.Json;
using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

[Collection(AspireCollection.Name)]
public sealed class AuthenticationTests(BlazorAppFixture app) : BffPageTest(app)
{
	private const string SessionCookie = "__Host-focus.session";

	[Fact]
	public async Task AnonymousVisitorLogsInAtKeycloakAndLandsOnThePageTheyAskedFor()
	{
		// The BFF pushes the authorization request (PAR), so the browser reaches Keycloak's authorize endpoint with a
		// request_uri handle instead of the parameters themselves. Keycloak then redirects on to its login form, so the
		// evidence is that first request, not the page the form ends up on.
		Task<IRequest> authorize = Page.WaitForRequestAsync(request =>
			request.Url.Contains("/protocol/openid-connect/auth?", StringComparison.Ordinal));
		Task<IRequest> callback = Page.WaitForRequestAsync(request =>
			request.Url.Contains("/signin-oidc", StringComparison.Ordinal));

		await Page.GotoAsync($"{App.BaseUrl}weather");

		Assert.Contains("request_uri=", (await authorize).Url, StringComparison.Ordinal);

		await BlazorAppFixture.LogInAsync(Page);

		// Keycloak posts the code back (form_post) instead of putting it into the callback's URL, where the browser
		// history and every access log on the way would keep it.
		Assert.Equal("POST", (await callback).Method);

		await Expect(Page).ToHaveURLAsync($"{App.BaseUrl}weather");
		await Expect(Page.GetByTestId("user-name")).ToHaveTextAsync(BlazorAppFixture.Username);
		await Expect(Page.GetByTestId("weather-table")).ToBeVisibleAsync();
	}

	[Fact]
	public async Task AnonymousVisitorSeesTheHomePage()
	{
		await Page.GotoAsync(App.BaseUrl);

		// Home's own content, which a redirect to Keycloak would never render, and the login where a user's name would be.
		await Expect(Page.GetByTestId("counter-value")).ToBeVisibleAsync();
		await Expect(Page.GetByTestId("login-button")).ToBeVisibleAsync();
	}

	[Fact]
	public async Task ApiRefusingTheTokenShowsAnErrorInsteadOfLoggingInAgain()
	{
		await using KeycloakAdmin keycloak = await App.SignInToKeycloakAsync(Playwright);

		// Tokens without the API's audience: the API refuses them, the session stays valid.
		await keycloak.RemoveProtocolMapperAsync("admin-bff", "admin-api-audience");

		await Page.GotoAsync(App.BaseUrl);
		await Page.GetByTestId("login-button").ClickAsync();
		await BlazorAppFixture.LogInAsync(Page);

		// A new login would bring back the same token, in a loop.
		Task<IRequest> login = Page.WaitForRequestAsync(
			request => request.Url.Contains("/bff/login", StringComparison.Ordinal),
			new PageWaitForRequestOptions { Timeout = 5000, });
		Task<IResponse> proxied = Page.WaitForResponseAsync(response =>
			response.Url.Contains("/_api/admin-api/", StringComparison.Ordinal));

		await Page.GetByTestId("nav-weather").ClickAsync();

		IResponse refused = await proxied;
		Assert.Equal(502, refused.Status);
		Assert.DoesNotContain("www-authenticate", (await refused.AllHeadersAsync()).Keys);

		await Assert.ThrowsAnyAsync<TimeoutException>(() => login);
		await Expect(Page).ToHaveURLAsync($"{App.BaseUrl}weather");
		await Expect(Page.Locator("#blazor-error-ui")).ToBeVisibleAsync();
	}

	public override BrowserNewContextOptions ContextOptions() => new() { IgnoreHTTPSErrors = true, };

	[Fact]
	public async Task EndingTheSessionAtKeycloakSignsOutOnTheNextPageLoad()
	{
		await using KeycloakAdmin keycloak = await App.SignInToKeycloakAsync(Playwright);

		// The BFF only notices an ended session when it renews the token.
		await keycloak.RenewAccessTokensOnEveryUseAsync();

		await Page.GotoAsync(App.BaseUrl);
		await Page.GetByTestId("login-button").ClickAsync();
		await BlazorAppFixture.LogInAsync(Page);

		// As an admin or another app's logout would, leaving the BFF's cookie behind.
		await keycloak.EndSessionAsync(await SessionIdAsync());
		await Page.ReloadAsync();

		await Expect(Page.GetByTestId("login-button")).ToBeVisibleAsync();
		Assert.DoesNotContain(
			await Page.Context.CookiesAsync(),
			cookie => cookie.Name.StartsWith(SessionCookie, StringComparison.Ordinal));
	}

	[Fact]
	public async Task LogoutEndsTheSessionAtKeycloakToo()
	{
		await Page.GotoAsync(App.BaseUrl);
		await Page.GetByTestId("login-button").ClickAsync();
		await BlazorAppFixture.LogInAsync(Page);

		await Page.GetByTestId("logout-button").ClickAsync();

		// Every chunk gone: a __Host- cookie is only deleted by a header that is itself Secure and on Path=/.
		await Expect(Page.GetByTestId("login-button")).ToBeVisibleAsync();
		Assert.DoesNotContain(
			await Page.Context.CookiesAsync(),
			cookie => cookie.Name.StartsWith(SessionCookie, StringComparison.Ordinal));

		// Logging in again shows the form rather than passing straight through: Keycloak's own session went with the cookie.
		await Page.GetByTestId("login-button").ClickAsync();
		await Expect(Page.Locator("#kc-login")).ToBeVisibleAsync();
	}

	[Fact]
	public async Task ProxiedApiCallsNeedTheSessionAndTheCsrfHeader()
	{
		await Page.GotoAsync(App.BaseUrl);
		await Page.GetByTestId("login-button").ClickAsync();
		await BlazorAppFixture.LogInAsync(Page);

		string url = $"{App.BaseUrl}_api/admin-api/weather-forecasts";
		Dictionary<string, string> csrfHeader = new() { ["X-CSRF"] = "1", };

		// Signed in, but without the header the app itself always sends: what a foreign page's call looks like.
		IAPIResponse withoutHeader = await Page.APIRequest.GetAsync(url);
		Assert.Equal(403, withoutHeader.Status);

		IAPIResponse withHeader = await Page.APIRequest.GetAsync(
			url,
			new APIRequestContextOptions { Headers = csrfHeader, });
		Assert.Equal(200, withHeader.Status);

		// No session at all: a status, never a redirect to Keycloak, which an XHR could not follow.
		await using IAPIRequestContext anonymous = await Playwright.APIRequest.NewContextAsync();
		IAPIResponse anonymousCall = await anonymous.GetAsync(
			url,
			new APIRequestContextOptions { Headers = csrfHeader, });
		Assert.Equal(401, anonymousCall.Status);
	}

	[Fact]
	public async Task SessionCookieIsASecureHostCookieEvenOverPlainHttp()
	{
		await Page.GotoAsync(App.BaseUrl);
		await Page.GetByTestId("login-button").ClickAsync();
		await BlazorAppFixture.LogInAsync(Page);

		// The suite reaches the BFF over http://localhost, where the cookie handler's SameAsRequest default leaves Secure off.
		// A large ticket is split into __Host-focus.session, __Host-focus.sessionC1, ..., each a cookie of its own.
		IReadOnlyList<BrowserContextCookiesResult> session =
		[
			.. (await Page.Context.CookiesAsync()).Where(cookie =>
				cookie.Name.StartsWith(SessionCookie, StringComparison.Ordinal)),
		];

		Assert.NotEmpty(session);
		Assert.All(session, cookie => Assert.True(cookie.Secure, $"{cookie.Name} lacks Secure."));
	}

	[Fact]
	public async Task UserEndpointAnswersAreNeverStored()
	{
		await Page.GotoAsync(App.BaseUrl);
		await Page.GetByTestId("login-button").ClickAsync();
		await BlazorAppFixture.LogInAsync(Page);

		IAPIResponse user = await Page.APIRequest.GetAsync($"{App.BaseUrl}bff/user");

		Assert.Equal(200, user.Status);
		Assert.Contains(
			"no-store",
			user.Headers.GetValueOrDefault("cache-control") ?? string.Empty,
			StringComparison.Ordinal);
	}

	// Keycloak's session id, the sid claim /bff/user returns.
	private async Task<string> SessionIdAsync()
	{
		IAPIResponse user = await Page.APIRequest.GetAsync($"{App.BaseUrl}bff/user");
		JsonElement body = await user.JsonAsync() ??
			throw new InvalidOperationException("/bff/user answered without a body.");

		return body.GetProperty("claims")
			.EnumerateArray()
			.Single(claim => claim.GetProperty("type").GetString() == "sid")
			.GetProperty("value")
			.GetString() ?? string.Empty;
	}
}
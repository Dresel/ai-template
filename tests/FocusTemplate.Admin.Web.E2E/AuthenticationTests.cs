using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

[Collection(AspireCollection.Name)]
public sealed class AuthenticationTests(BlazorAppFixture app) : BffPageTest(app)
{
	[Fact]
	public async Task AnonymousVisitorLogsInAtKeycloakAndLandsOnThePageTheyAskedFor()
	{
		// The BFF pushes the authorization request (PAR), so the browser reaches Keycloak's authorize endpoint with a
		// request_uri handle instead of the parameters themselves. Keycloak then redirects on to its login form, so the
		// evidence is that first request, not the page the form ends up on.
		Task<IRequest> authorize = Page.WaitForRequestAsync(request =>
			request.Url.Contains("/protocol/openid-connect/auth?", StringComparison.Ordinal));

		await Page.GotoAsync($"{App.BaseUrl}weather");

		Assert.Contains("request_uri=", (await authorize).Url, StringComparison.Ordinal);

		await BlazorAppFixture.LogInAsync(Page);

		await Expect(Page).ToHaveURLAsync($"{App.BaseUrl}weather");
		await Expect(Page.GetByTestId("user-name")).ToHaveTextAsync(BlazorAppFixture.Username);
		await Expect(Page.GetByTestId("weather-table")).ToBeVisibleAsync();
	}

	public override BrowserNewContextOptions ContextOptions() => new() { IgnoreHTTPSErrors = true, };

	[Fact]
	public async Task LogoutEndsTheSessionAtKeycloakToo()
	{
		await Page.GotoAsync(App.BaseUrl);
		await BlazorAppFixture.LogInAsync(Page);

		await Page.GetByTestId("logout-button").ClickAsync();

		// The login form again rather than the app: Keycloak's own session went with the cookie.
		await Expect(Page.Locator("#kc-login")).ToBeVisibleAsync();
	}

	[Fact]
	public async Task ProxiedApiCallsNeedTheSessionAndTheCsrfHeader()
	{
		await Page.GotoAsync(App.BaseUrl);
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
}
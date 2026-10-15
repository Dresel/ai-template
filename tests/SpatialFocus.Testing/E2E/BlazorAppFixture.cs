using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace SpatialFocus.Testing.E2E;

// The product's AppHost, started once for a collection, and a signed-in session. The product creates the builder with
// its feature flags pinned and names the BFF, the Keycloak resource and the realm.
public abstract class BlazorAppFixture(string bffResource, string keycloakResource, string realm) : IAsyncLifetime
{
	// What the token's name claim says, the realm file's first and last name, and so what the API calls the user
	public const string DisplayName = "Dev Eloper";

	public const string Password = "developer";

	// The developer login every product's realm file imports
	public const string Username = "developer";

	private DistributedApplication? app;

	public string BaseUrl { get; private set; } = string.Empty;

	public string StorageState { get; private set; } = string.Empty;

	public static Task LogInAsync(IPage page) => LogInAsync(page, Username, Password);

	public static async Task LogInAsync(IPage page, string username, string password)
	{
		await SubmitLogInAsync(page, username, password);
		await page.GetByTestId("user-name").WaitForAsync();
	}

	public static Task SubmitLogInAsync(IPage page) => SubmitLogInAsync(page, Username, Password);

	// Keycloak's login theme: the field ids are stable across its versions, unlike the markup around them.
	public static async Task SubmitLogInAsync(IPage page, string username, string password)
	{
		await page.FillAsync("#username", username);
		await page.FillAsync("#password", password);
		await page.ClickAsync("#kc-login");
	}

	public async ValueTask DisposeAsync()
	{
		if (this.app is not null)
		{
			await this.app.DisposeAsync();
		}

		GC.SuppressFinalize(this);
	}

	public async ValueTask InitializeAsync()
	{
		using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(180));

		IDistributedApplicationTestingBuilder builder = await CreateBuilderAsync(cancellationTokenSource.Token);

		this.app = await builder.BuildAsync(cancellationTokenSource.Token);
		await this.app.StartAsync(cancellationTokenSource.Token);
		await this.app.ResourceNotifications.WaitForResourceHealthyAsync(bffResource, cancellationTokenSource.Token);

		BaseUrl = this.app.GetEndpoint(bffResource, "http").ToString();
		StorageState = await CaptureSessionAsync();
	}

	// As Keycloak's own admin, whose password the AppHost generates.
	public async Task<KeycloakAdmin> SignInToKeycloakAsync(IPlaywright playwright)
	{
		DistributedApplication started =
			this.app ?? throw new InvalidOperationException("The AppHost has not started.");
		KeycloakResource keycloak = started.Services.GetRequiredService<DistributedApplicationModel>()
			.Resources.OfType<KeycloakResource>()
			.Single();

		string? userName = keycloak.AdminUserNameParameter is { } parameter
			? await parameter.GetValueAsync(CancellationToken.None)
			: null;
		string password = await keycloak.AdminPasswordParameter.GetValueAsync(CancellationToken.None) ??
			throw new InvalidOperationException("Keycloak's admin password has no value.");

		// "admin" is the integration's default when the AppHost names no admin user.
		return await KeycloakAdmin.SignInAsync(
			playwright,
			started.GetEndpoint(keycloakResource, "http"),
			realm,
			userName ?? "admin",
			password);
	}

	protected abstract Task<IDistributedApplicationTestingBuilder> CreateBuilderAsync(CancellationToken cancellationToken);

	// One real login per run. Every test context starts from this state, so only AuthenticationTests pays for the flow.
	private async Task<string> CaptureSessionAsync()
	{
		using IPlaywright playwright = await Playwright.CreateAsync();
		await using IBrowser browser = await playwright.Chromium.LaunchAsync();
		await using IBrowserContext context =
			await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true, });

		IPage page = await context.NewPageAsync();
		await page.GotoAsync(BaseUrl);
		await page.GetByTestId("login-button").ClickAsync();
		await LogInAsync(page);

		return await context.StorageStateAsync();
	}
}
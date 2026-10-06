using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Projects;

namespace FocusTemplate.Admin.Web.E2E;

public sealed class BlazorAppFixture : IAsyncLifetime
{
	// What the token's name claim says, the realm file's first and last name, and so what the API calls the user
	public const string DisplayName = "Dev Eloper";

	public const string Password = "developer";

	// The developer login of the imported realm, src/FocusTemplate.AppHost/keycloak/focus-realm.json.
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
	}

	public async ValueTask InitializeAsync()
	{
		using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(180));

		IDistributedApplicationTestingBuilder builder =
			await DistributedApplicationTestingBuilder.CreateAsync<FocusTemplate_AppHost>(
				[
					"Features:TlsOffloadingIngress=false",
					"Features:Analytics=false",
					"Features:Mobile=false",
					"Features:LocalKeycloak=true",
					"Features:PersistentLocalKeycloak=false",
					"Features:PersistentDatabase=false",
					"Features:Chaos=false",
				],
				cancellationTokenSource.Token);

		this.app = await builder.BuildAsync(cancellationTokenSource.Token);
		await this.app.StartAsync(cancellationTokenSource.Token);
		await this.app.ResourceNotifications.WaitForResourceHealthyAsync("admin-bff", cancellationTokenSource.Token);

		BaseUrl = this.app.GetEndpoint("admin-bff", "http").ToString();
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
			started.GetEndpoint("keycloak", "http"),
			userName ?? "admin",
			password);
	}

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
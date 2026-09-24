using Microsoft.Playwright;
using Projects;

namespace FocusTemplate.Admin.Web.E2E;

public sealed class BlazorAppFixture : IAsyncLifetime
{
	public const string Password = "developer";

	// The developer login of the imported realm, src/FocusTemplate.AppHost/keycloak/focus-realm.json.
	public const string Username = "developer";

	private DistributedApplication? app;

	public string BaseUrl { get; private set; } = string.Empty;

	public string StorageState { get; private set; } = string.Empty;

	// Keycloak's login theme: the field ids are stable across its versions, unlike the markup around them.
	public static async Task LogInAsync(IPage page)
	{
		await page.FillAsync("#username", Username);
		await page.FillAsync("#password", Password);
		await page.ClickAsync("#kc-login");

		await page.GetByTestId("user-name").WaitForAsync();
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
				["Features:TlsOffloadingIngress=false", "Features:Analytics=false", "Features:Mobile=false",],
				cancellationTokenSource.Token);

		this.app = await builder.BuildAsync(cancellationTokenSource.Token);
		await this.app.StartAsync(cancellationTokenSource.Token);
		await this.app.ResourceNotifications.WaitForResourceHealthyAsync("admin-bff", cancellationTokenSource.Token);

		BaseUrl = this.app.GetEndpoint("admin-bff", "http").ToString();
		StorageState = await CaptureSessionAsync();
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
		await LogInAsync(page);

		return await context.StorageStateAsync();
	}
}
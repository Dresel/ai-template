using OpenQA.Selenium.Appium;
using Projects;

namespace FocusTemplate.Public.Mobile.E2E;

public sealed class AppiumFixture : IAsyncLifetime
{
	private const string AppPackage = "com.focustemplate.mobile";

	// Baked into the APK via android-test-env.targets, the environment maps it to the real API port.
	private const int DeviceApiPort = 5210;

	private DistributedApplication? app;
	private AppiumSession? appium;
	private MobileTestEnvironment? environment;

	public AppiumDriver? Driver => this.appium?.Driver;

	public string? SkipReason { get; private set; }

	public async ValueTask DisposeAsync()
	{
		if (this.appium is not null)
		{
			try
			{
				await this.appium.DisposeAsync();
			}
			catch (Exception)
			{
				// Keep disposing - the environment and the AppHost must be cleaned up regardless.
			}
		}

		if (this.environment is not null)
		{
			await this.environment.DisposeAsync();
		}

		if (this.app is not null)
		{
			await this.app.DisposeAsync();
		}
	}

	public AppiumDriver EnsureReady()
	{
		if (SkipReason is not null)
		{
			Assert.Skip(SkipReason);
		}

		return this.appium!.RestartApp();
	}

	public async ValueTask InitializeAsync()
	{
		MobileAppUnderTest appUnderTest = new(
			AppPackage,
			Path.Combine(
				TestPaths.RepoRoot,
				"src",
				"public",
				"FocusTemplate.Public.Mobile",
				"FocusTemplate.Public.Mobile.csproj"),
			Path.Combine(AppContext.BaseDirectory, "android-test-env.targets"));

		if (AndroidTestEnvironment.TryCreate(
			appUnderTest,
			out AndroidTestEnvironment? android,
			out string? androidSkipReason))
		{
			this.environment = android;
		}
		else if (IosTestEnvironment.TryCreate(appUnderTest, out IosTestEnvironment? ios, out string? iosSkipReason))
		{
			this.environment = ios;
		}
		else
		{
			SkipReason = $"{androidSkipReason} (iOS: {iosSkipReason})";
			return;
		}

		if (!AppiumSession.TryFindEntryPoint(out FileInfo? appiumMainJs, out string? appiumSkipReason))
		{
			SkipReason = appiumSkipReason;
			return;
		}

		this.app = await StartAppHostAsync();

		int apiPort = this.app.GetEndpoint("public-api", "http").Port;
		this.environment.ConfigureBackendAccess(DeviceApiPort, apiPort);
		await this.environment.InstallAppAsync();

		this.appium = AppiumSession.Start(this.environment, AppPackage, appiumMainJs);
	}

	private static async Task<DistributedApplication> StartAppHostAsync()
	{
		using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(180));

		IDistributedApplicationTestingBuilder builder =
			await DistributedApplicationTestingBuilder.CreateAsync<FocusTemplate_AppHost>(
				["Features:TlsOffloadingIngress=false", "Features:Analytics=false", "Features:Mobile=false",],
				cancellationTokenSource.Token);

		DistributedApplication app = await builder.BuildAsync(cancellationTokenSource.Token);
		await app.StartAsync(cancellationTokenSource.Token);
		await app.ResourceNotifications.WaitForResourceHealthyAsync("public-api", cancellationTokenSource.Token);
		return app;
	}
}
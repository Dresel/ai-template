using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Service;

namespace FocusTemplate.Public.Mobile.E2E;

public sealed class AppiumSession : IAsyncDisposable
{
	private readonly string applicationId;
	private readonly AppiumLocalService service;

	private AppiumSession(AppiumLocalService service, AppiumDriver driver, string applicationId)
	{
		this.service = service;
		Driver = driver;

		this.applicationId = applicationId;
	}

	public AppiumDriver Driver { get; }

	public static AppiumSession Start(MobileTestEnvironment environment, string applicationId, FileInfo appiumMainJs)
	{
		AppiumServiceBuilder serviceBuilder = new AppiumServiceBuilder().WithAppiumJS(appiumMainJs)
			.WithIPAddress("127.0.0.1")
			.UsingAnyFreePort()
			.WithStartUpTimeOut(TimeSpan.FromSeconds(60));

		if (environment.AppiumServerEnvironment.Count > 0)
		{
			serviceBuilder =
				serviceBuilder.WithEnvironment(new Dictionary<string, string>(environment.AppiumServerEnvironment));
		}

		AppiumLocalService service = serviceBuilder.Build();
		service.Start();
		try
		{
			AppiumDriver driver = environment.CreateDriver(service.ServiceUrl, TimeSpan.FromMinutes(3));
			return new AppiumSession(service, driver, applicationId);
		}
		catch (Exception)
		{
			service.Dispose();
			throw;
		}
	}

	public static bool TryFindEntryPoint(
		[NotNullWhen(true)] out FileInfo? appiumMainJs,
		[NotNullWhen(false)] out string? skipReason)
	{
		string appiumEntryPoint = Path.Combine(TestPaths.ProjectDirectory, "node_modules", "appium", "index.js");
		if (!File.Exists(appiumEntryPoint))
		{
			appiumMainJs = null;
			skipReason =
				"Appium not found - run 'npm ci' in tests/FocusTemplate.Public.Mobile.E2E once (installs the pinned Appium + UiAutomator2 driver).";
			return false;
		}

		appiumMainJs = new FileInfo(appiumEntryPoint);
		skipReason = null;

		return true;
	}

	public ValueTask DisposeAsync()
	{
		try
		{
			Driver.Quit();
		}
		catch (Exception)
		{
			// Best-effort, the server is disposed below either way.
		}

		this.service.Dispose();

		return ValueTask.CompletedTask;
	}

	public AppiumDriver RestartApp()
	{
		Driver.TerminateApp(this.applicationId);
		Driver.ActivateApp(this.applicationId);

		return Driver;
	}
}
using System.Collections.ObjectModel;
using OpenQA.Selenium.Appium;

namespace FocusTemplate.Public.Mobile.E2E;

public abstract class MobileTestEnvironment : IAsyncDisposable
{
	public virtual IReadOnlyDictionary<string, string> AppiumServerEnvironment =>
		ReadOnlyDictionary<string, string>.Empty;

	public abstract void ConfigureBackendAccess(int devicePort, int hostPort);

	public abstract AppiumDriver CreateDriver(Uri appiumServerUrl, TimeSpan commandTimeout);

	public abstract ValueTask DisposeAsync();

	public abstract Task InstallAppAsync();
}
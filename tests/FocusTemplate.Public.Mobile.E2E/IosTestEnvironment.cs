using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium.Appium;

namespace FocusTemplate.Public.Mobile.E2E;

public sealed class IosTestEnvironment : MobileTestEnvironment
{
	private IosTestEnvironment()
	{
	}

	public static bool TryCreate(
		MobileAppUnderTest appUnderTest,
		[NotNullWhen(true)] out IosTestEnvironment? environment,
		[NotNullWhen(false)] out string? skipReason)
	{
		// Signature parity with AndroidTestEnvironment.TryCreate, used once the harness is implemented.
		_ = appUnderTest;

		environment = null;
		skipReason = OperatingSystem.IsMacOS()
			? "iOS harness not implemented yet."
			: "iOS needs a macOS host (Xcode/simulator).";
		return false;
	}

	public override void ConfigureBackendAccess(int devicePort, int hostPort) =>
		throw new NotImplementedException("iOS harness not implemented yet.");

	public override AppiumDriver CreateDriver(Uri appiumServerUrl, TimeSpan commandTimeout) =>
		throw new NotImplementedException("iOS harness not implemented yet.");

	public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

	public override Task InstallAppAsync() => throw new NotImplementedException("iOS harness not implemented yet.");
}
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AndroidSdk;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;

namespace FocusTemplate.Public.Mobile.E2E;

public sealed class AndroidTestEnvironment : MobileTestEnvironment
{
	private readonly Adb adb;
	private readonly MobileAppUnderTest appUnderTest;
	private readonly List<int> reversedDevicePorts = [];

	private AndroidTestEnvironment(string sdkHome, Adb adb, MobileAppUnderTest appUnderTest)
	{
		this.adb = adb;
		this.appUnderTest = appUnderTest;
		this.AppiumServerEnvironment = new Dictionary<string, string> { ["ANDROID_HOME"] = sdkHome, };
	}

	public override IReadOnlyDictionary<string, string> AppiumServerEnvironment { get; }

	public static bool TryCreate(
		MobileAppUnderTest appUnderTest,
		[NotNullWhen(true)] out AndroidTestEnvironment? environment,
		[NotNullWhen(false)] out string? skipReason)
	{
		environment = null;
		try
		{
			IReadOnlyList<DirectoryInfo> sdkHomes = new SdkLocator().Locate();
			DirectoryInfo? sdkHome = sdkHomes.Count > 0 ? sdkHomes[0] : null;
			if (sdkHome is null)
			{
				skipReason = "Android SDK not found (ANDROID_HOME unset, no default install location).";
				return false;
			}

			Adb adb = new(sdkHome);

			// The app is deployed with -p:AdbTarget=-e, so specifically an emulator must be running.
			if (!adb.GetDevices().Any(device => device.IsEmulator))
			{
				skipReason = "No Android emulator connected (adb devices) - start an emulator to run the mobile E2E suite.";
				return false;
			}

			environment = new AndroidTestEnvironment(sdkHome.FullName, adb, appUnderTest);
			skipReason = null;
			return true;
		}
		catch (Exception exception)
		{
			skipReason = $"Android toolchain unavailable: {exception.Message}";
			return false;
		}
	}

	public override void ConfigureBackendAccess(int devicePort, int hostPort)
	{
		ProcessResult result = this.adb.RunCommand("reverse", $"tcp:{devicePort}", $"tcp:{hostPort}");
		if (!result.Success)
		{
			throw new InvalidOperationException($"adb reverse failed: {result.GetAllOutput()}");
		}

		this.reversedDevicePorts.Add(devicePort);
	}

	// -t:Install builds the APK with the baked test environment and deploys it to the emulator
	// (-p:AdbTarget=-e). Cold builds can take minutes, incremental ones are fast.
	public override Task InstallAppAsync() =>
		ExecAsync(
			"dotnet",
			$"build \"{this.appUnderTest.ProjectPath}\" -f net11.0-android -t:Install -p:AdbTarget=-e \"-p:CustomAfterMicrosoftCommonTargets={this.appUnderTest.AndroidEnvironmentTargetsFile}\"",
			TimeSpan.FromMinutes(10));

	public override AppiumDriver CreateDriver(Uri appiumServerUrl, TimeSpan commandTimeout)
	{
		// The MAUI activity name is a mangled crc64 type name - resolve it instead of hardcoding.
		string activity = this.adb.Shell($"cmd package resolve-activity --brief {this.appUnderTest.ApplicationId}")
			.Last(line => line.Contains('/', StringComparison.Ordinal))
			.Trim()
			.Split('/')[1];

		AppiumOptions options = new()
		{
			PlatformName = "Android",
			AutomationName = "UiAutomator2",
		};
		options.AddAdditionalAppiumOption("appPackage", this.appUnderTest.ApplicationId);
		options.AddAdditionalAppiumOption("appActivity", activity);
		options.AddAdditionalAppiumOption("noReset", true);
		options.AddAdditionalAppiumOption("newCommandTimeout", 300);

		// Session creation installs the UiAutomator2 server on the device and launches the app.
		return new AndroidDriver(appiumServerUrl, options, commandTimeout);
	}

	public override ValueTask DisposeAsync()
	{
		foreach (int devicePort in this.reversedDevicePorts)
		{
			try
			{
				this.adb.RunCommand("reverse", "--remove", $"tcp:{devicePort}");
			}
			catch (Exception)
			{
				// Best-effort cleanup. The mapping dies with the emulator/adb server anyway.
			}
		}

		return ValueTask.CompletedTask;
	}

	private static async Task<(int ExitCode, string Output)> RunAsync(string fileName, string arguments, TimeSpan timeout)
	{
		using Process process = new();
		process.StartInfo = new ProcessStartInfo
		{
			FileName = fileName,
			Arguments = arguments,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		process.Start();
		Task<string> stdout = process.StandardOutput.ReadToEndAsync();
		Task<string> stderr = process.StandardError.ReadToEndAsync();

		using CancellationTokenSource cancellation = new(timeout);
		try
		{
			await process.WaitForExitAsync(cancellation.Token);
		}
		catch (OperationCanceledException)
		{
			process.Kill(entireProcessTree: true);
			throw new TimeoutException($"'{fileName} {arguments}' timed out after {timeout}.");
		}

		return (process.ExitCode, await stdout + await stderr);
	}

	private static async Task<string> ExecAsync(string fileName, string arguments, TimeSpan timeout)
	{
		(int exitCode, string output) = await RunAsync(fileName, arguments, timeout);
		return exitCode == 0
			? output
			: throw new InvalidOperationException($"'{fileName} {arguments}' failed with exit code {exitCode}:{Environment.NewLine}{output[^Math.Min(output.Length, 4000)..]}");
	}
}
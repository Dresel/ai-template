# Mobile

What to follow when changing these files: [the rule](../.claude/rules/mobile.md).

## Build

- `Microsoft.Maui.Controls` takes its version from the installed MAUI workload (`VersionOverride="$(MauiVersion)"`).
  `Microsoft.Maui.Core` is pinned in `Directory.Packages.props` and must match the workload band
  (`dotnet workload list`).
- Building the solution needs the `maui-android` and `maui-ios` workloads and the Android SDK platform matching the band
  (`dotnet build -t:InstallAndroidDependencies` installs it).

## Dev loop

- `Features:Mobile` on in `appsettings.local.json` adds the device resources and a Dev Tunnel that exposes
  `public-api` to the emulator (anonymous, dev only). The first start asks to install and sign in to the `devtunnel`
  CLI.
- Android needs a running emulator (`adb devices`). Boot one with `emulator -avd <name>` and wait with
  `adb wait-for-device shell 'while [ "$(getprop sys.boot_completed)" != "1" ]; do sleep 2; done'`. The loop runs on
  the device, so the host command stays a single allowlisted `adb` call. A host-side loop around `adb` prompts.
- The iOS simulator resource shows "unsupported" on Windows, since it runs from a macOS host only.

## Tests

- `FocusTemplate.Public.Mobile.E2E` runs native flows on the Android emulator through Appium and UiAutomator2. It boots
  the AppHost, builds and installs the APK on every run with a test environment baked in, and maps the API with
  `adb reverse` instead of a Dev Tunnel.
- Appium and its driver are npm devDependencies of the test project: one `npm ci` there once. Without them, or without
  an emulator, the tests skip themselves.
- Locators: a MAUI `AutomationId` becomes the Android `resource-id`, so tests use `MobileBy.Id("<AutomationId>")`, and the
  driver prefixes the app package. `AccessibilityId` does not match. Every interactive control gets an `AutomationId`.

## Bugs and features

Red first in `FocusTemplate.Public.Mobile.E2E`. The `appium` MCP drives the emulator interactively (find, tap, type,
screenshot, page source) with the tests' locator semantics, so an interactive repro translates directly into the failing
test. The server is configured in `.mcp.json` but not enabled in the project settings (`enabledMcpjsonServers`), so it
has to be enabled for mobile work. New UI starts with its `AutomationId`s, which the red test asserts against. Pure
visuals (layout, theming) are checked by emulator screenshot without a persistent test. Raw `adb` is the fallback and the
evidence channel: `adb exec-out screencap -p`, `adb shell uiautomator dump`, `adb logcat`.
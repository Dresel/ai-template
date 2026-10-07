---
paths:
  - "src/public/*.Public.Mobile*/**"
  - "tests/*.Public.Mobile.E2E/**"
---
# Mobile

- Every interactive control has an `AutomationId`, which tests locate with `MobileBy.Id("<AutomationId>")`.
- New UI starts with its `AutomationId`s and a red test in `FocusTemplate.Public.Mobile.E2E`. Pure visuals are checked
  by emulator screenshot.
- Drive the emulator through the `appium` MCP, with raw `adb` as fallback. Wait for boot with the device-side loop, so
  the host command stays a single `adb` call.
- `Microsoft.Maui.Core` matches the installed workload band, and `Microsoft.Maui.Controls` takes
  `VersionOverride="$(MauiVersion)"`.
- The dev loop needs `Features:Mobile` in `appsettings.local.json`. The E2E suite maps the API with `adb reverse`.

Background: [docs/mobile.md](../../docs/mobile.md).
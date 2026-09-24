# FocusTemplate — MAUI / Public vertical

*Last updated: 2026-07-16. Companion to [backlog.md](backlog.md) §8. Records the concrete state of the
native-MAUI mobile client and its `Public.Api` backend after the initial spike.*

**Status:** verified end-to-end on the Android emulator (2026-07-16): dashboard Start → build →
deploy → app launch → weather list via Dev Tunnel → OTLP trace in the dashboard
(`mobile-android-emulator → devtunnel → public-api → focusdb` incl. SQL). Two launch bugs were found
and worked around — see **Device launch: verified behavior & workaround** below.

---

## What exists

Two audience verticals now share one domain (`FocusTemplate.Data`). The **Public** vertical lives in
`src/public/`:

| Project | Role |
|---|---|
| `FocusTemplate.Public.Api` | Exposed minimal API for external clients. `ServiceDefaults` + `AddNpgsqlDbContext("focusdb")`; one `/weatherforecast` read endpoint mapping entity → DTO. Mirrors `Admin.Api`. |
| `FocusTemplate.Public.Shared` | Mobile wire contract (`WeatherForecastResponse`). Must never reference `Admin.Shared`. |
| `FocusTemplate.Public.Mobile` | Native .NET MAUI app (XAML). Targets **`net11.0-android;net11.0-ios` only**. |
| `FocusTemplate.Public.Mobile.ServiceDefaults` | MAUI-side Aspire defaults (service discovery, resilience, OTel) — from the `maui-aspire-servicedefaults` template. No ASP.NET Core dependency. |

The MAUI app: a typed `WeatherApiClient` (`GetFromJsonAsync` over `https+http://public-api`) injected
into `MainPage`, which renders a `CollectionView` weather list on button tap. `AutomationId` on the
button (`LoadWeatherButton`) and list (`WeatherList`) — the native equivalent of `data-testid`.

## Request flow (mobile)

```
Android emulator / iOS simulator ──▶ Dev Tunnel ──▶ public-api ──▶ Postgres (EF Core)
        (device can't reach localhost)   (anonymous, dev-only)      ▲
                                                          migrations ┘ (WaitForCompletion)
```

- Windows/Mac Catalyst desktop reach localhost directly, but this template has **no desktop targets** —
  it's mobile-only by decision, so every device target needs the Dev Tunnel.
- OTLP telemetry from the device is tunnelled separately via `WithOtlpDevTunnel()` so traces/metrics
  land in the Aspire dashboard.

## AppHost wiring

All behind **`Features:Mobile`** (`appsettings.json`, default **`false`**), so a plain `aspire start`
and the E2E suite carry no devtunnel/emulator/workload requirements. Override in the gitignored
`appsettings.local.json`; the E2E fixture pins it off via CLI arg.

```csharp
if (addMobile)
{
    var mobile = builder.AddMauiProject(
        "mobile", "../public/FocusTemplate.Public.Mobile/FocusTemplate.Public.Mobile.csproj");

    var devTunnel = builder.AddDevTunnel("devtunnel")
        .WithAnonymousAccess()
        .WithReference(publicApi.GetEndpoint("http"));

    mobile.AddAndroidEmulator().WithOtlpDevTunnel().WithReference(publicApi, devTunnel);
    mobile.AddiOSSimulator().WithOtlpDevTunnel().WithReference(publicApi, devTunnel);
}
```

- **`AddMauiProject` takes a csproj *path string*, never a `ProjectReference`** — incompatible TFMs
  would break the AppHost build (same class of trap as the Data `NoWarn ASPIRE004`).
- `public-api` itself is wired **unconditionally** (not behind the flag); only the MAUI + tunnel
  resources are gated.
- Device configs are additive; incompatible ones (iOS simulator on Windows) show **"unsupported"** in
  the dashboard rather than failing.
- Resource names: `public-api`, `mobile`, `devtunnel`.

## Versions & workloads

- **`Aspire.Hosting.Maui` `13.4.6-preview.1.26319.6`** (preview — keep in lockstep with the AppHost SDK,
  same rule as `Aspire.Hosting.Blazor`/`.EntityFrameworkCore`) + **`Aspire.Hosting.DevTunnels` `13.4.6`**.
- **`Microsoft.Maui.Controls`** uses `VersionOverride="$(MauiVersion)"` — the version is resolved from
  the installed MAUI workload, not CPM.
- **`Microsoft.Maui.Core`** is CPM-pinned in `Directory.Packages.props` to the workload band
  (`11.0.0-preview.5.26304.4`); **must match `dotnet workload list`** or restore fails with NU1605/NU1109.
- Workloads: `maui-android` + `maui-ios` (preview.5 band) installed into the SDK; the pre-existing
  VS-installed `maui-windows` + platform SDKs were not sufficient.
- **Android SDK platform 37** (preview) required (min deploy target raised to `24.0`); installed via
  `dotnet build -t:InstallAndroidDependencies -f net11.0-android -p:AcceptAndroidSDKLicenses=True`.

## Device launch: verified behavior & workaround (2026-07-16)

Reported symptom confirmed: clicking **Start** on `mobile-android-emulator` in the dashboard did
nothing — resource went straight to *Finished* (pid 0, start == stop timestamp, no console output).
Two independent causes, diagnosed via the DCP API (`kubectl --kubeconfig %TEMP%\aspire-dcp*\kubeconfig
get executable <name> -o yaml`):

1. **VS-launched AppHost silently no-ops MAUI device starts.** When the AppHost runs under Visual
   Studio (F5/Ctrl-F5), every project resource gets DCP `executionType: IDE` — DCP delegates the
   launch to VS's run-session host. VS 2026 doesn't implement MAUI device launches: with a
   `launchSettings.json` present it acks the request and reports *Finished* immediately (the known
   "VS integration incomplete" preview gap), so `fallbackExecutionTypes: [Process]` never kicks in.
   **→ `launchSettings.json` was deleted from the MAUI project** (it only held the useless
   "Windows Machine" profile — no Windows TFM): VS then fails the request loudly (500 Internal
   Server Error) and DCP falls back to spawning the process itself. Alternative: start the AppHost
   with `aspire start` (CLI), where everything runs as `executionType: Process` from the start.
2. **The `run` verb: required or forbidden depending on host.** `Aspire.Hosting.Maui`
   13.4.6-preview composes device-resource args for a bare `dotnet` command (`run -f
   net11.0-android -p:AdbTarget=-e ...`), but DCP launches the resource differently per host:
   - **CLI (project composition):** `dotnet run --project <csproj> --configuration Debug
     --no-launch-profile <args>` — the package's leading `run` duplicates the verb; the Android run
     tool exits 1 with `Error: Unexpected argument(s): run` after a successful build + deploy.
   - **VS fallback (direct spawn):** `dotnet <args>` verbatim in the project directory — here the
     `run` verb is *required* (without it: `dotnet -f ...` → "dotnet--f does not exist").
   **→ Conditional workaround in `AppHost.cs`:** `Args.Remove("run")` only when
   `DEBUG_SESSION_PORT` is unset (i.e. not IDE-hosted). Upstream `main` has already reworked the
   whole launch pipeline (build queue + `dotnet build /t:Run -p:NoBuild=true`), so re-check and
   drop the workaround at the next `Aspire.Hosting.Maui` bump.

Verified: CLI path end-to-end (see below). VS path: fallback spawn reaches `dotnet` correctly with
the conditional workaround, full deploy-from-dashboard under VS still to be confirmed.
**Regressed 2026-08-27 (behavior change vs. July, still on 13.4.6):** under a VS-launched AppHost
the device start now no-ops *silently* — no 500, no `Process` fallback: resource goes straight to
*Finished* (pid 0, start == stop, env targets generated, no spawn/MSBuild output), despite
`launchSettings.json` still being absent. Suspected cause: VS updates since July (18.9/18.10) now
ack the launch instead of failing it, so `fallbackExecutionTypes: [Process]` never triggers.
Practical rule: **run the AppHost via `aspire start` for the mobile loop**.

**Update 2026-08-27 — Aspire 13.5.3 bump, new pipeline, new bug + workaround:** the launch pipeline
was reworked upstream as anticipated: a build-queue pass builds the project, then the device
resource spawns `dotnet build --no-restore /t:Run -p:NoBuild=true …` (composed via a
`ProjectLaunchArgsOverrideAnnotation`, which strips the leading `run` itself) — the July
duplicate-`run` workaround is obsolete and was **dropped** from `AppHost.cs`. New bug: Android's
target chain `Run → Install → SignAndroidPackage` explicitly `DependsOn` **Build** outside VS
(`Microsoft.Android.Sdk.BuildOrder.targets`, gated on `BuildingInsideVisualStudio != 'true'`), so
the SDK guard kills every launch with **NETSDK1085** ("The 'NoBuild' property was set to true but
the 'Build' target was invoked"). Workaround in `AppHost.cs`: **append `-p:NoBuild=false` via
`WithArgs`** — user args land after the override args and MSBuild's last `-p` wins; the second
Build stays incremental because the build-queue pass just ran. `context.Args.Remove(...)` does
*not* work — the override args never pass through the `WithArgs` context. Verified end-to-end
(build → deploy → app relaunched with new PID). Upstream:
[microsoft/aspire#18724](https://github.com/microsoft/aspire/issues/18724) (open since 2026-07-10,
regression vs. 13.4.6, no fix/workaround documented there). Fix is in flight:
[aspire#19383](https://github.com/microsoft/aspire/pull/19383) (successor of draft
[#18591](https://github.com/microsoft/aspire/pull/18591), milestone 13.6) drops `-p:NoBuild=true`
for Android entirely — semantically the same as this workaround (non-Android keeps `NoBuild=true`
plus `-p:BuildDependsOn=`). Once that ships, our appended `-p:NoBuild=false` becomes a no-op —
drop it at the 13.6 bump.

**VS path verified working on 13.5.3 (2026-08-27):** with the reworked pipeline the VS-launched
no-op is gone — a VS(F5)-hosted AppHost (parent chain `devenv → VsDebugConsole → AppHost`) now
spawns the device resource through DCP as a real process (`[sys] Starting process…`), the
unconditional `-p:NoBuild=false` workaround applies there too, and build → deploy → app launch
succeed. Both hosts (CLI and VS) now work; the workaround is deliberately *not* gated on
`DEBUG_SESSION_PORT`.

**Stale-APK trap when comparing runs:** the app bakes its OTLP/service-discovery env into the APK
at build time (`__aspire_environment__.txt`), and Dev-Tunnel URLs are stable across AppHost
restarts (tunnel IDs derive from the AppHost hash). An app instance deployed by an *earlier* run
keeps sending telemetry into the *current* dashboard session — "app runs and logs OTel" does not
prove the dashboard Start worked. The device telemetry also shows up as its own OTLP application
(`mobile-android-emulator-<guid-prefix>`) whenever a DCP-run instance of the resource exists in the
same session, because the package bakes a per-run GUID `service.instance.id` (by design, same as
Docker Compose / Azure App Service; no upstream issue for this).

Verified flow after the workaround (AppHost via `aspire start`, emulator running via `adb devices`):
dashboard Start streams the full MSBuild output into resource console logs, resource state
*Running* with a real PID, app deploys and launches (`com.focustemplate.mobile`), the weather list
loads through the tunnel, and the distributed trace (device HTTP client span → `public-api` →
Postgres SQL) appears in the dashboard. The env targets file
(`%TEMP%\aspire-maui-android-env*\*.targets` with OTLP + service-discovery URLs) is regenerated on
every Start, so fresh tunnel URLs are picked up automatically.

## Gotchas encountered (net11 preview, 2026-07)

- The MAUI project templates still emit **net10-era content**: default TFMs, `SupportedOSPlatformVersion`
  `21` (Android now requires `24`), and a `maui-aspire-servicedefaults` `Extensions.cs` missing
  `using` directives (`IMauiInitializeService` via `Microsoft.Maui.Hosting`, `AddServiceDiscovery` via
  `Microsoft.Extensions.DependencyInjection`).
- `Directory.Build.props` pins a single repo-wide `<TargetFramework>net11.0</TargetFramework>`; the MAUI
  csproj must **clear it with an empty `<TargetFramework></TargetFramework>`** before setting
  `<TargetFrameworks>`, or multi-targeting silently collapses.
- **`dotnet format` corrupts multi-TFM projects** — it processes each TFM as a separate project and
  writes git-style conflict markers into shared source. **Never run it on `FocusTemplate.Public.Mobile`;**
  hand-format or use `dotnet jb cleanupcode --include=`. (Recorded in AGENTS.md.)
- Android `ApplicationId` segments must be valid Java identifiers — `public` is a keyword, so the id is
  `com.focustemplate.mobile`, not `...public.mobile`.
- `Page.DisplayAlert` is obsolete in net11 MAUI → `DisplayAlertAsync`.
- Template platform bootstrap files (`MainActivity`, `MainApplication`, `AppDelegate`, `Program`) trip
  StyleCop/IDE rules (IDE0130 namespace-vs-folder, CA1711 "AppDelegate" ends in "Delegate"); resolved
  with scoped `#pragma warning disable` since the names are platform conventions.

## Testing

- **Existing suite green**: `dotnet build FocusTemplate.slnx` (both MAUI TFMs compile — iOS *compiles*
  on Windows, *links* only on macOS) + `dotnet test` (incl. the Playwright E2E that boots the full
  AppHost with `public-api` running, `Features:Mobile=false`).
- **`FocusTemplate.Public.Mobile.E2E`** *(added 2026-07-16)* — the Appium smoke tier (backlog §8.4).
  The fixture boots the AppHost (`Features:Mobile=false`), builds + installs the APK with a baked
  test env (`android-test-env.targets` → `SERVICES__PUBLIC-API__HTTP__0=http://localhost:5210`,
  mapped by `adb reverse` to the real port — no Dev Tunnel/login in tests; `localhost` also suits a
  future iOS-simulator harness), spawns an Appium server, and drives the app via UiAutomator2.
  Prereqs: running emulator + one-time `npm ci` in the test project — Appium and the UiAutomator2
  driver are **project-local npm devDependencies** (pinned via `package.json`/`package-lock.json`,
  same spirit as CPM/`.config/dotnet-tools.json`; the official MAUI docs use a global install, the
  dotnet/maui repo itself provisions locally). Without emulator or `node_modules` the suite
  **auto-skips** so plain `dotnet test` stays green. The fixture's SDK/adb plumbing (SDK discovery,
  device listing, `adb reverse`, activity resolution) is the [`AndroidSdk`](https://github.com/Redth/AndroidSdk.Tools)
  NuGet (`SdkLocator`/`Adb`) — the library the MAUI team's own `dotnet android` tool builds on; its
  `Emulator`/`AvdManager` classes are the designated path for a future opt-in auto-boot/CI step. **Locator convention:** MAUI
  `AutomationId` = Android `resource-id` → `MobileBy.Id("<AutomationId>")` — the UiAutomator2 driver
  auto-qualifies bare ids with the app package (verified); `AccessibilityId` does *not* match.
- **Not yet built**: `Public.Api.IntegrationTests` (Testcontainers, same contract as
  `Admin.Api.IntegrationTests`) — see backlog §8.2.

## Not yet done / next steps

1. ~~**Actual emulator run**~~ — **done 2026-07-16**, see *Device launch* section above.
   `appsettings.local.json` with `Features:Mobile=true` is in place (gitignored).
2. `Public.Api.IntegrationTests` + the `FocusTemplate.IntegrationTesting` extraction (backlog 8.2).
3. ~~Appium smoke test~~ — **done 2026-07-16** as `FocusTemplate.Public.Mobile.E2E` (Android; iOS
   harness pending a macOS runner/BrowserStack), see Testing above.
4. Auth (backlog 3.2): mobile = Keycloak public client, code + PKCE → bearer → `Public.Api` JwtBearer.
5. Track upstream (both bugs already reported, open, no milestone as of 2026-07-16):
   - Duplicate `run` verb: [#15248](https://github.com/microsoft/aspire/issues/15248) and
     [#16919](https://github.com/microsoft/aspire/issues/16919) — both document the same
     `.WithArgs(ctx => ctx.Args.Remove("run"))` workaround we applied; the launch-pipeline rework in
     [PR #18591](https://github.com/microsoft/aspire/pull/18591) should supersede it.
   - VS-delegated device start: [#12943](https://github.com/microsoft/aspire/issues/12943) — same
     root cause (VS launches via the selected launch profile instead of the device target;
     confirmed still broken in Aspire 13.1.3 / VS 2026 18.4.1). Community workaround there:
     delete `launchSettings.json`, then start via dashboard (untested here); our approach is
     starting the AppHost with `aspire start` instead of VS. Related: VS Code iOS equivalent was
     fixed in [PR #17857](https://github.com/microsoft/aspire/pull/17857).

## Risks (re-check at every Aspire/MAUI bump)

- `Aspire.Hosting.Maui` is **preview**, VS 2026 integration incomplete.
- MAUI's 2026 quality situation (Android 16 edge-to-edge regressions) — pin workload versions.
- MAUI support policy: each major supported only ~6 months after its successor → forced annual mobile
  upgrades; the Tier-2 Appium smoke suite is the intended regression net.

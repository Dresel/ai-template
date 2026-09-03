# FocusTemplate — progress summary

*Last updated: 2026-08-27*

An AI-first, modern **.NET 11** template on **.NET Aspire**. The architecture is deliberately shaped so an AI agent can propose a feature, implement it, and *verify it itself* — backend and frontend independently — using Aspire's live introspection and a fast testing pyramid. Since 2026-07-14 the solution is split into two **audience verticals** sharing one domain: **Admin** (internal-only API/BFF/WASM) and **Public** (deliberately exposed API + native MAUI mobile client).

## Solution layout

Shared spine:

| Project | Role |
|---|---|
| `FocusTemplate.AppHost` | Aspire orchestration (dev). Runs Postgres, the EF migration resource, both APIs, the BFF, optional nginx ingress + optional Umami analytics, and (behind `Features:Mobile`) the MAUI device resources + Dev Tunnel (feature flags). |
| `FocusTemplate.Data` | EF Core data layer: `AppDbContext`, entities, `Migrations/`, design-time factory, dev seed (`WeatherSeed`). One domain, referenced by both APIs. |
| `FocusTemplate.ServiceDefaults` | Shared server defaults: OpenTelemetry, health checks, service discovery, resilience. |

Admin vertical (`src/admin/`):

| Project | Role |
|---|---|
| `FocusTemplate.Admin.Api` | Internal minimal API (`/weatherforecast`) — reads Postgres via EF Core (`AppDbContext`), maps entity → DTO. |
| `FocusTemplate.Admin.Web` | Blazor **WASM** frontend (standalone, Blazorise Material). |
| `FocusTemplate.Admin.Web.Bff` | Thin **BFF** (ASP.NET Core + YARP): hosts the WASM, reverse-proxies `/_api/api`, proxies `/_otlp`, serves `/_blazor/_configuration` (Aspire client config) + `/config` (app config), dev-only `/debug/request`. |
| `FocusTemplate.Admin.Web.ClientServiceDefaults` | WASM-side defaults: client OpenTelemetry (reads `OTEL_*` env vars) + JS initializer (`lib.module.js`) that bootstraps them. |
| `FocusTemplate.Admin.Shared` | Admin wire contract (`WeatherForecastResponse`). Entities never leave the server. |

Public vertical (`src/public/`):

| Project | Role |
|---|---|
| `FocusTemplate.Public.Api` | Exposed minimal API for external clients (mobile); same EF Core access to the shared domain, its own audience-shaped DTOs. Mirrors `Admin.Api`. |
| `FocusTemplate.Public.Mobile` | Native .NET MAUI app (XAML), **`net11.0-android;net11.0-ios` only** (no Windows/MacCatalyst by decision). |
| `FocusTemplate.Public.Mobile.ServiceDefaults` | MAUI counterpart of ServiceDefaults (service discovery, resilience, OTel), from the `maui-aspire-servicedefaults` template; no ASP.NET Core dependency. |
| `FocusTemplate.Public.Shared` | Mobile wire contract. The two `Shared` projects never reference each other. |

Tests:

| Project | Role |
|---|---|
| `tests/FocusTemplate.Admin.Api.IntegrationTests` | `WebApplicationFactory` + **Testcontainers Postgres**; database-per-test-class, Respawn reset per test. |
| `tests/FocusTemplate.Admin.Web.E2E` | Playwright E2E (xUnit v3) driven **through the BFF**; boots the full AppHost incl. migrations + seed. |
| `tests/FocusTemplate.Public.Mobile.E2E` | Appium/UiAutomator2 E2E on the Android emulator; boots the AppHost, builds + installs the APK with a baked test env (`adb reverse`, no Dev Tunnel); auto-skips without npm deps or an emulator. |

## Architecture & request flow

Single public entrypoint for the browser is the **nginx ingress**; everything internal is plain HTTP (TLS terminated at the edge). The Public API is deliberately exposed for external clients; in dev the emulator reaches it through a Dev Tunnel:

```
browser ──HTTPS──▶ nginx ingress ──http──▶ BFF ──http──▶ admin-api ──▶ Postgres
                   (TLS offload)          (YARP /api)    (EF Core)      ▲  ▲
                                                       migrations ──────┘  │ (one-shot, seeds in dev)
mobile (emulator) ──▶ devtunnel ──▶ public-api ────────────────────────────┘
                      (dev only)    (EF Core)
```

- **`WithTlsOffloadingIngress()`** (`AppHost/IngressExtensions.cs`) — reusable extension that puts an nginx container in front of a project. Derives the name (`<resource>-ingress`), injects the upstream host/port (`APP_HOST`/`APP_PORT` via `EndpointProperty`), provisions the dev cert with **`WithHttpsDeveloperCertificate()`**, and enables forwarded headers on the target.
- **BFF is internal-only**; **API is internal-only**; only the ingress is external. `UseHttpsRedirection` removed from internal hops.
- **YARP routes are emitted by the AppHost** via `Aspire.Hosting.Blazor` (see below): `/_api/api/*` → API, `/_otlp/*` → dashboard OTLP receiver.

## Public vertical + native MAUI mobile (2026-07-14 → 2026-08-27)

Details in [maui.md](maui.md); decision record in [backlog.md](backlog.md) §8. **Verified end-to-end
on the Android emulator (2026-07-16)**: dashboard Start → build → deploy → app launch → weather list
via Dev Tunnel → distributed OTLP trace `mobile-android-emulator → devtunnel → public-api → focusdb`
incl. SQL spans.

- **Rename first** (commit `a6193a9`): the original API/BFF/WASM stack moved under `src/admin/` as
  the Admin vertical; the new Public vertical lives in `src/public/` (projects in the layout table
  above). Naming is audience/exposure-based; Blazor Hybrid was rejected in favor of native MAUI XAML.
- **AppHost wiring**: `public-api` is always on (references the shared `focusdb`, waits for
  migrations). Everything device-related sits behind **`Features:Mobile` (default off)** — a plain
  `aspire start` needs no devtunnel CLI or emulator; developers opt in via the gitignored
  `appsettings.local.json`. When on: `AddMauiProject` (`Aspire.Hosting.Maui`, **preview**, lockstep
  with the AppHost SDK) + `AddDevTunnel().WithAnonymousAccess()` exposing `public-api` to devices,
  then `AddAndroidEmulator()`/`AddiOSSimulator()` with `WithOtlpDevTunnel()` and
  `WithReference(publicApi, devTunnel)`. The iOS simulator resource shows "unsupported" on Windows.
- **Launch workarounds (upstream bugs)**: the Aspire CLI launches device resources via
  `dotnet run --project …`, which breaks MAUI device targets → strip the leading `run` arg
  (aspire#15248/#16919); VS (detected via `DEBUG_SESSION_PORT`) needs `run` preserved (#12943).
- **MAUI app**: typed `WeatherApiClient` (`GetFromJsonAsync` over `https+http://public-api`,
  service discovery from `Public.Mobile.ServiceDefaults`) injected into `MainPage`; `AutomationId`
  on every interactive control — the native equivalent of `data-testid`.
- **Version discipline**: `Microsoft.Maui.Controls` comes from the installed workload via
  `VersionOverride="$(MauiVersion)"`; `Microsoft.Maui.Core` is CPM-pinned and **must match the
  workload band** (`dotnet workload list`). **Never `dotnet format` the multi-TFM MAUI project** —
  it processes each TFM separately and writes conflict markers; use `dotnet jb cleanupcode --include=`.
- **Mobile E2E** (`tests/FocusTemplate.Public.Mobile.E2E`): Appium/UiAutomator2 through the .NET
  `Appium.WebDriver` client; Appium server + driver are **project-local npm devDependencies**
  (one-time `npm ci`). The fixture boots the AppHost (`Features:Mobile=false` — no Dev Tunnel),
  builds + installs the APK with a baked test env (`android-test-env.targets`) and bridges via
  `adb reverse`; auto-skips without npm deps or an emulator. Locators: MAUI `AutomationId` =
  Android `resource-id` → `MobileBy.Id(...)`.
- **Interactive driving** for repro/verification: the `appium` MCP (find/tap/type/screenshot/page
  source) with the same locator semantics as the tests; raw `adb` (`screencap`, `uiautomator dump`,
  `logcat` tag `template.mobile`) as fallback + evidence channel.

## Postgres + EF Core + Aspire migrations (2026-07-08 → 2026-07-14)

- **Data layer** (`FocusTemplate.Data`): `AppDbContext`, `WeatherForecast` entity (POCO, convention-mapped), `Migrations/` (StyleCop-exempt via a **local `Migrations/.editorconfig`** with `generated_code = true`), design-time `AppDbContextFactory` (reads `ConnectionStrings__focusdb` / `Database__SeedTestData` from env — no live DB needed for `migrations add`).
- **AppHost wiring**: `AddPostgres("postgres").AddDatabase("focusdb")` (no data volume — every start is fresh + re-seeded, deterministic) → API `WithReference` + `WaitFor`. Schema is applied by **`AddEFMigrations("api-migrations")`** (`Aspire.Hosting.EntityFrameworkCore` *preview*, superseded the hand-rolled migration-worker pattern): `RunDatabaseUpdateOnStart()` in run mode, `api.WaitForCompletion(...)` gates the API, `PublishAsMigrationBundle(publishContainer: true)` emits an idempotent bundle container for prod (ACA Job / compose `restart:no`; k8s Job via publisher customization). Dashboard commands (Add Migration, Update/Reset/Drop DB, Status) come free.
- **Gotcha (empirically found)**: env vars for the migration run must go on the **tool resource** via `configureToolResource` — `WithEnvironment` on the migration resource never reaches the spawned `dotnet ef` process. That's how the dev-seed flag (`Database__SeedTestData`) travels.
- **Dev seed**: `WeatherSeed` via EF `UseSeeding`/`UseAsyncSeeding` — **both** delegates (the EF CLI calls the sync one), idempotent (insert-if-empty), deterministic 5-row dataset, **run mode only** (published bundle never seeds). E2E asserts the seeded values through the full stack — the seed is verified via the *production* seeding path.
- **DTO/entity split**: `WeatherForecastResponse` (Shared) is the wire contract; the entity stays server-side; mapping happens in the endpoint. `/weatherforecast` now reads from Postgres ordered by date (random generator deleted).
- **Versions**: EF Core stack **pinned to 10.0.9** (stable, runs on net11; Aspire's transitive 10.0.8 unified via CPM), Npgsql provider 10.0.2, repo-local `dotnet-ef` 10.0.9. **EF 11 previews exist but the Npgsql provider caps EF at `< 11`** — when `Npgsql.EntityFrameworkCore.PostgreSQL 11.x` ships, move runtime + Design + Relational + provider + `dotnet-ef` as one unit. `Microsoft.EntityFrameworkCore.Design` (`PrivateAssets="all"`) lives in **both** Data (factory interface, local `migrations add`) and Api (the `AddEFMigrations` tool boots with the API as startup project).
- **AppHost references Data** only for `WithMigrationsProject<>` — `NoWarn ASPIRE004` (setting `IsAspireProjectResource=false` would kill the generated `Projects.*` type).

## Integration test architecture (final, after several iterations)

**One container, one database per test class, fresh state per test** — every isolation property holds structurally, nothing by convention:

- `PostgresFixture` (assembly fixture): one Postgres container + `CreateDatabaseAsync(name)`. Only **`CREATE DATABASE` is semaphore-serialized** (concurrent creations contend on the template DB); migrations run in parallel.
- `ApiFixture` (class fixture — xUnit instantiates one **per test class**): provisions its own GUID-named DB, migrates it (`MigrateAsync` creates missing DBs itself), builds its own Respawner, binds the API host to it. Test classes share nothing → full parallelism, **no xUnit collections**.
- `ApiTestBase`: per-test `ResetAsync()` (Respawn, FK-safe, `__EFMigrationsHistory` preserved). Tests **arrange exactly what they assert** (`Factory.CreateDbContext()`); the dev seed is never used in integration tests.
- Design history worth keeping: a **read/write two-database split** (seeded-immutable read DB + serialized write collection) was built first, then deliberately removed — the seeded pattern's only real advantage is *seed once, read parallel*, which pays only when arrange-cost × read-test-count outgrows the contract discipline it demands (documented in AGENTS.md as the future option). Also removed: order-dependent "isolation proof" tests (replaced by a self-contained `ResetClearsAllRows`) and a redundant 200-check.
- Transaction-rollback isolation is not viable here (API opens its own connection via `WebApplicationFactory`).

## Adopted from the Aspire playground samples (2026-07-02)

Migrated to the official pattern from [`playground/BlazorHosted`](https://github.com/microsoft/aspire/tree/v13.4.6/playground/BlazorHosted) / [`BlazorStandalone`](https://github.com/microsoft/aspire/tree/v13.4.6/playground/BlazorStandalone) (compared against the **`v13.4.6` tag**). Our shape is "standalone but hosted": a standalone WASM project served by a developer-owned BFF.

- **`Aspire.Hosting.Blazor` `13.4.6-preview.1.26319.6`** (experimental `ASPIREBLAZOR001`; keep in lockstep with the AppHost SDK — same rule now applies to `Aspire.Hosting.EntityFrameworkCore`).
- **`ProxyBlazorService(api)`** emits the YARP `/_api/api/*` route + cluster env vars; **`ProxyBlazorTelemetry()`** emits `/_otlp/*` → dashboard with the OTLP API key as server-side header transform (key never reaches the browser).
- **Client config delivery**: `Client__ConfigResponse`/`Client__ConfigEndpointPath` → BFF serves `/_blazor/_configuration` → JS initializer injects Mono env vars before .NET starts.
- **Deliberate deviations**: page-origin-relative `_api/api/...` (CORS), no client `AddServiceDiscovery()`, `/config` kept for app-level browser config (Analytics), `NullLogger` in `BackgroundExportHandler` (aspire#18272, gated on preview6).

## OpenTelemetry

- **Server OTel** (API, BFF) via ServiceDefaults → Aspire dashboard (dev). EF/Npgsql spans come free via `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` (`AddNpgsqlDbContext`: health checks + OTel + retries).
- **Client OTel** (WASM): working end-to-end (browser → ingress → BFF `/_otlp` → dashboard; distributed trace `bff (client)` → `bff` → `api`). Browser uses `http/protobuf`, path-only endpoint. Under `Aspire.Hosting.Testing` (no dashboard) client telemetry disables cleanly.
- Export failures client-side still invisible (`NullLogger` + synthetic 200; #18272).

## Testing

- **API integration**: Testcontainers Postgres (real provider, real migrations), architecture above — green (3 tests). **Requires Docker.**
- **E2E** (Playwright through the ingress): Counter + Weather — green; Weather asserts the **deterministic seed values** end-to-end (Postgres → API → BFF → WASM DOM). Boots the full AppHost incl. `api-migrations`.
- **Mobile E2E** (Appium/UiAutomator2, see the Public vertical section): weather smoke test on the Android emulator; auto-skips without `npm ci` in the test project or a running emulator.
- `dotnet test FocusTemplate.slnx` is the one-shot verification (stop the AppHost first — bin lock).

## Tooling & conventions

- .NET 11 preview5 SDK; **Aspire 13.4.6**; Central Package Management + lock files; StyleCop + strict analyzers; tabs/CRLF.
- `AGENTS.md` grew substantially: **"check current Aspire docs first"** rule (stale model knowledge is how the superseded worker pattern almost got built), Database & migrations conventions, Integration test database contract, permission-system traps.
- **Permissions (hard-won, verified model)**: rules merge across scopes with precedence **deny → ask → allow** (a global `ask` overrides a project `allow`); pipelines auto-run when **every segment** matches an allow rule; a matching *deny* blocks silently — a repeated *prompt* means an ask rule, an unmatched command, or a **dead allowlist**. Read-only cmdlets (`Select-Object`, `ConvertFrom-Json`, …), `dnx dotnet-inspect *`, and read-only `aspire docs/doctor/export/config get|list|info` are allow-listed. The project permission set is mirrored into the user-global `~/.claude/settings.json` as the default for new projects.
- **Run the app via the Aspire CLI** (`aspire start`/`wait`/`stop`), evidence via Aspire MCP (`list_resources`, `list_console_logs`, …), in-place `rebuild` for code changes.

## Known issues & gotchas

- **Workspace trust silently disables the project allowlist** (root cause of a long permission-debugging saga): on Windows, `~/.claude.json` registers the same folder under **two path spellings** (`C:\…` CLI vs `C:/…` desktop app) with independent `hasTrustDialogAccepted`; the untrusted entry suppresses every project `allow` rule while built-in read-only heuristics keep working — which masks it. Check **both** entries. Upstream: anthropics/claude-code#27706/#19910 (closed without fix); focused issue drafted + fixed locally for all projects.
- **`Aspire.Hosting.Blazor` / `Aspire.Hosting.EntityFrameworkCore` are experimental/preview**: re-check against the matching `v*` tag when updating Aspire; bump in lockstep with the SDK.
- **Client-side OTLP export failures are silent** (#18272, gated on preview6).
- **Ports change every restart** — grab current URLs from `aspire describe`/dashboard.
- **`git clean -xfd` deletes all untracked files** — commit work so it survives.
- An interrupted `aspire start` can leave **orphaned `FocusTemplate.*` processes** that lock build output (MSB3026/3027) — also happens with a running AppHost; stop it before `dotnet build`/`test`.
- **Docker must be running** for integration tests + E2E (doesn't auto-start after reboot).

## Production OTLP (documented, not yet implemented)

Everything the AppHost emits in dev is plain `IConfiguration` on the BFF — prod supplies the same keys from ConfigMap/Secret (`Client__ConfigEndpointPath`/`Client__ConfigResponse`, YARP cluster destinations, standard `OTEL_*` env vars on pods). Precedence: appsettings < env vars, so AppHost values win in dev automatically.

## Roadmap / next steps

**Open items moved to [backlog.md](backlog.md)** (2026-07-14) — compiled from the feature wishlist
(Mapperly, DDD/Vogen, Postgres enums, FusionCache, Scalar, Keycloak BFF auth, eventing, OTel metrics,
UI flow) + research over SSW VSA, the Clean Architecture templates, eShop, and fullstackhero.

- [x] **Postgres + EF Core** — done (data project, `AddEFMigrations`, seed, Testcontainers suite; see sections above).
- [x] **Umami analytics** (CommunityToolkit) — integrated behind `Features:Analytics`.
- [x] **Aspire CLI lifecycle documented** in `AGENTS.md` (skill + MCP toolbox table).
- [x] **Public vertical + native MAUI mobile** (backlog §8) — 8.1 rename, 8.2 `Public.Api` skeleton, 8.3 MAUI + Aspire wiring, 8.4 Tier-2 Android Appium suite; verified end-to-end on the emulator. Open: 8.2 hardening + `Public.Api.IntegrationTests`, iOS test lane, DeviceRunners spike (❓8.4).

# Architecture

How the projects connect. Each area has its own doc, listed in the index in [AGENTS.md](../AGENTS.md).

## Request paths

- **Admin**: browser → BFF (`FocusTemplate.Admin.Web.Bff`) → API (`FocusTemplate.Admin.Api`) → Postgres. The BFF serves
  the WASM client and proxies `/_api/*` to the API with the user's access token, `/_otlp/*` to the Aspire dashboard and
  `/_analytics/*` to Umami. The browser only ever talks to the BFF, which holds the session (see
  [authentication](authentication.md)). The API is never exposed to the browser.
- **Public**: the MAUI app (`FocusTemplate.Public.Mobile`) → `FocusTemplate.Public.Api` → Postgres, read-only through
  the weather feature's `ReadOnlyWeatherDbContext`, with a contract of its own shaped for the mobile client. The API
  is anonymous until the mobile client's PKCE leg.
- **Telemetry**: every server reports OpenTelemetry through ServiceDefaults, the WASM client through
  `FocusTemplate.Admin.Web.ClientServiceDefaults` and the BFF's `/_otlp`, the MAUI app through
  `FocusTemplate.Public.Mobile.ServiceDefaults` (the MAUI counterpart of ServiceDefaults, without ASP.NET Core).

## Spine, verticals and features

- **The spine** is what every host has: `Primitives`, `Http` (the generated library types), `Data.Foundation`,
  `Data` (identity: users, groups, permissions), `Api.Foundation`, `Admin.Web.Foundation`, the service defaults and
  the AppHost.
- **A vertical** is an audience with its own API, contract and client: Admin, Public. Its slices live inside its
  projects (`Features/<Slice>/`).
- **A feature** is a package of projects under `src/features/<name>/` that a host adds without touching its own
  code beyond composition. Weather is the first:

  | Project | Holds | References |
  |---|---|---|
  | `spec/` | `api.tsp`, a `@service` of its own, `primitives.tsp` with its typed ids, `permissions.tsp`, and the editor's `tspconfig.yaml` | the spine's `primitives.tsp` |
  | `FocusTemplate.Weather.Shared` | the generated wire contract and its partials | `Http`, `Primitives` |
  | `FocusTemplate.Weather.Client` | the generated typed clients | `Weather.Shared` |
  | `FocusTemplate.Weather.Data` | entities, `WeatherDbContext` and its read-only twin in schema `weather`, migrations, the dev seed | `Data.Foundation`, `Primitives` |
  | `FocusTemplate.Weather.Api` | handlers, the generated endpoints with `MapWeatherEndpoints()`, `AddWeather()` | `Api.Foundation`, `Weather.Data`, `Weather.Shared` |
  | `FocusTemplate.Weather.Web` | the pages, a Razor class library, `AddWeatherPages()` | `Admin.Web.Foundation`, `Admin.Web.ClientServiceDefaults`, `Weather.Client` |

  A feature never references the host's projects (`Data`, `Admin.Api`, `Admin.Shared`, `Admin.Client`,
  `Admin.Web`, `Public.Api`); `FeatureTests` checks it. What it needs of identity comes from the spine: `ICurrentUser`,
  `UserReference` for a foreign key to the users, `PermissionRequirement` answered by the host's handler.

- **Composition** in the host, one line each: `builder.AddWeather("focusdb", "focusdb-readonly")` and
  `endpoints.MapWeatherEndpoints()` in `Admin.Api`, `AddWeatherPages(environment, "admin-api")` and the assembly in
  the `Router`'s `AdditionalAssemblies` in `Admin.Web`, `AddMigrations<WeatherDbContext>(…)` with
  `.WaitForCompletion(migrations)` in the AppHost (see [database](database.md)), the feature's `primitives.tsp` imported
  by `src/spec/primitives.tsp`, and the menu entry in `MainLayout`. A host without the feature leaves out these lines and the project references.

## Projects not covered by an area doc

- **FocusTemplate.Admin.Client**, **FocusTemplate.Public.Client**: the generated typed HTTP clients of each vertical,
  referenced by every consumer, so the client an app ships is the one the tests drive.
- **FocusTemplate.Admin.Shared**, **FocusTemplate.Public.Shared**: each vertical's wire contract, generated records plus
  hand-written partials for computed members. They never reference each other. `Admin.Shared` also holds what both
  Admin ends need to speak a language (`Localization/`, see [localization](localization.md)).
- **FocusTemplate.Public.Mobile**: native MAUI with XAML, `net11.0-android;net11.0-ios` only (no Windows or
  MacCatalyst targets by decision, and iOS builds only on macOS), see [mobile](mobile.md).

## Tests

| Project | What it runs on |
|---|---|
| `FocusTemplate.Admin.Api.IntegrationTests` | `Microsoft.AspNetCore.Mvc.Testing`, Testcontainers Postgres and Redis, Respawn, `FakeTimeProvider`, see [integration tests](integration-tests.md) |
| `FocusTemplate.Admin.Web.E2E` | `Aspire.Hosting.Testing` booting the AppHost, Playwright, see [web E2E](web-e2e.md) |
| `FocusTemplate.Admin.Web.UnitTests` | plain xUnit, components rendered with `HtmlRenderer`, see [web forms](web-forms.md) |
| `FocusTemplate.ArchitectureTests` | reflection over the API assemblies and the EF model, see [database](database.md) |
| `FocusTemplate.Public.Mobile.E2E` | Appium with UiAutomator2 on the Android emulator, see [mobile](mobile.md) |

All use xUnit v3 on Microsoft.Testing.Platform.
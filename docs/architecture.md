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
  `SpatialFocus.Web.ClientServiceDefaults` and the BFF's `/_otlp`, the MAUI app through
  `FocusTemplate.Public.Mobile.ServiceDefaults` (the MAUI counterpart of ServiceDefaults, without ASP.NET Core).
  The client's `BlazorClientExtensions` and its JS initializer follow `BlazorStandalone.ClientServiceDefaults` in
  Aspire's playground ([microsoft/aspire](https://github.com/microsoft/aspire/tree/main/playground/BlazorStandalone)),
  and an update of Aspire or OpenTelemetry starts by comparing against it. Ours takes the service name as an argument,
  registers no service discovery, since every call goes to the BFF's own origin, and loads the analytics script.

## Spine, verticals and features

- **The spine** is what the hosts build on: `Primitives`, `Http` (the generated library types), `Data.Foundation`,
  `Data` (identity: users, groups, permissions), `Api.Foundation`, `Web.Foundation`, `Web.ClientServiceDefaults`,
  `Localization`, the BFF's contract `Web.Bff.Shared`, the hosting libraries (`AppHost.Hosting`, `Api.Hosting`,
  `Web.Bff.Hosting`, `Web.Hosting`), the service defaults and the AppHost.
- **References** run from the product down to what it shares: a `SpatialFocus.*` project references no
  `FocusTemplate.*` one, and a shared project without a vertical in its name references none of a vertical's. A
  product's BFF hands `Web.Bff.Hosting` its permission source, the one piece of user management the BFF needs.
  `ProjectReferenceTests` checks both rules from the project files.
- **A vertical** is an audience with its own API, contract and client: Admin, Public. Its slices live inside its
  projects (`Features/<Slice>/`).
- **User management** (`src/admin/user-management/`) is the Admin vertical's part of the spine: every Admin host
  includes it, since the BFF asks its `/users/me` on each page load and every `@requiresPermission` goes to its
  permission handler. Its projects are laid out as a feature's below (`spec/` with a `@service` of its own,
  `SpatialFocus.Admin.UserManagement.Shared`, `.Client`, `.Api`, `.Web`), without a data project: it works on the
  identity tables of `Data`. `AddUserManagement(connectionName, readOnlyConnectionName)` registers the spine's two
  context pools, its endpoints' JSON context and validators, `UserPermissions` with the permission handler, the memory
  cache of the provisioning and the `GroupChanged` signal. `app.UseUserProvisioning()` adds the provisioning, which the
  host runs between authentication and authorization. `MapUserManagementEndpoints()` maps its endpoints,
  `AddUserManagementPages(…)` registers its pages and their `PageAssembly`.
  A feature never references it.
- **A feature** is a package of projects under `src/features/<name>/` that a host adds without touching its own
  code beyond composition. A package fits a self-contained capability that hosts drop in: shared between products,
  likely to be, or standing on its own with jobs and pages (Lamama's monitoring, Focuma's releases). A product's own
  business is a slice of its vertical, with its tables in a context of the product's own in `FocusTemplate.Data`.
  Weather is the template's example of a package. On its own merits it would be a slice of the Admin vertical, since
  it serves one product, runs no jobs and shares nothing:

  | Project | Holds | References |
  |---|---|---|
  | `spec/` (`SpatialFocus.Weather.Spec`) | `api.tsp`, a `@service` of its own, `primitives.tsp` with its typed ids, `permissions.tsp`, and the editor's `tspconfig.yaml` | the spine's `primitives.tsp` |
  | `SpatialFocus.Weather.Primitives` | the feature's typed ids and `WeatherPermissions` (generated), `StationId.New()` | `Primitives` |
  | `SpatialFocus.Weather.Shared` | the generated wire contract and its partials | `Http`, `Primitives`, `Weather.Primitives` |
  | `SpatialFocus.Weather.Client` | the generated typed clients | `Weather.Shared` |
  | `SpatialFocus.Weather.Data` | entities, `WeatherDbContext` and its read-only twin in schema `weather`, migrations, the dev seed | `Data.Foundation`, `Primitives`, `Weather.Primitives` |
  | `SpatialFocus.Weather.Api` | handlers, the generated endpoints with `MapWeatherEndpoints()`, `AddWeather()` | `Api.Foundation`, `Weather.Data`, `Weather.Shared` |
  | `SpatialFocus.Weather.Web` | the pages, a Razor class library, `AddWeatherPages()` | `Web.Foundation`, `Web.ClientServiceDefaults`, `Weather.Client` |

  A feature references its own projects and the spine's foundations alone (`Primitives`, `Http`, `Data.Foundation`,
  `Api.Foundation`, `Web.Foundation`, `Web.ClientServiceDefaults`). `FeatureTests` checks it against that
  list, which keeps `Data`, user management, the hosting libraries and the host's projects out of a feature's reach,
  and a new shared project too until it is added there. What it needs of identity comes from the spine: `ICurrentUser`,
  `UserReference` for a foreign key to the users, `PermissionRequirement` answered by user management's handler.

- **Composition** in the host, one line each:
  `builder.AddWeather(ConnectionNames.Database, ConnectionNames.ReadOnlyDatabase)` and
  `endpoints.MapWeatherEndpoints()` in `Admin.Api`, `AddWeatherPages(environment, ResourceNames.AdminApi)` in
  `Admin.Web`, which also hands the feature's pages to the `Router`,
  `AddMigrations("weather-migrations", "SpatialFocus.Weather.Data.WeatherDbContext", …)` with
  `.WaitForCompletion(migrations)` in the AppHost (see [database](database.md)), the feature's `primitives.tsp`
  imported by the product's `src/admin/spec/primitives.tsp`, and the menu entry in `MainLayout`. A feature with permissions changes `AdminPermissions.All`, so adding or removing
  it is also a migration in `FocusTemplate.Data`. A host without the feature leaves out these lines and the project
  references. The feature takes the host's names as arguments, so it never references `FocusTemplate.Names`.

## Projects not covered by an area doc

- **FocusTemplate.Admin.Client**, **FocusTemplate.Public.Client**: the generated typed HTTP clients of each vertical,
  referenced by every consumer, so the client an app ships is the one the tests drive.
- **FocusTemplate.Admin.Shared**, **FocusTemplate.Public.Shared**: each vertical's wire contract, generated records plus
  hand-written partials for computed members. They never reference each other. `Admin.Shared` also holds the
  languages the app speaks (`Localization/Cultures`, see [localization](localization.md)) and the answer of the BFF's
  `/debug/request` (`Diagnostics/RequestDiagnostics`).
- **SpatialFocus.Localization**: the localization machinery the API and the WASM client both run, a plain library
  with no vertical in its name (see [localization](localization.md)).
- **SpatialFocus.Web.Bff.Shared**: the BFF's contract with the WASM client, `/bff/user`'s answer, the claim types and the
  analytics settings of the client configuration (see [authentication](authentication.md)).
- **FocusTemplate.Public.Mobile**: native MAUI with XAML, `net11.0-android;net11.0-ios` only (no Windows or
  MacCatalyst targets by decision, and iOS builds only on macOS), see [mobile](mobile.md).

## Tests

| Project | What it runs on |
|---|---|
| `FocusTemplate.Admin.Api.IntegrationTests` | `Microsoft.AspNetCore.Mvc.Testing`, Testcontainers Postgres and Redis, Respawn, `FakeTimeProvider`, see [integration tests](integration-tests.md) |
| `FocusTemplate.Admin.Web.E2E` | `Aspire.Hosting.Testing` booting the AppHost, Playwright, see [web E2E](web-e2e.md) |
| `SpatialFocus.Web.UnitTests` | plain xUnit over Foundation, user management and the features, components rendered with `HtmlRenderer`, see [web forms](web-forms.md) |
| `FocusTemplate.Admin.Web.UnitTests` | plain xUnit over the host's own slices, see [web forms](web-forms.md) |
| `FocusTemplate.ArchitectureTests` | reflection over the API assemblies and the EF model, see [database](database.md) |
| `FocusTemplate.Public.Mobile.E2E` | Appium with UiAutomator2 on the Android emulator, see [mobile](mobile.md) |

All use xUnit v3 on Microsoft.Testing.Platform.
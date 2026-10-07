# Architecture

How the projects connect. Each area has its own doc, listed in the index in [AGENTS.md](../AGENTS.md).

## Request paths

- **Admin**: browser → BFF (`FocusTemplate.Admin.Web.Bff`) → API (`FocusTemplate.Admin.Api`) → Postgres. The BFF serves
  the WASM client and proxies `/_api/*` to the API with the user's access token, `/_otlp/*` to the Aspire dashboard and
  `/_analytics/*` to Umami. The browser only ever talks to the BFF, which holds the session (see
  [authentication](authentication.md)). The API is never exposed to the browser.
- **Public**: the MAUI app (`FocusTemplate.Public.Mobile`) → `FocusTemplate.Public.Api` → Postgres, read-only through
  `ReadOnlyAppDbContext`, with a contract of its own shaped for the mobile client. The API is anonymous until the
  mobile client's PKCE leg.
- **Telemetry**: every server reports OpenTelemetry through ServiceDefaults, the WASM client through
  `FocusTemplate.Admin.Web.ClientServiceDefaults` and the BFF's `/_otlp`, the MAUI app through
  `FocusTemplate.Public.Mobile.ServiceDefaults` (the MAUI counterpart of ServiceDefaults, without ASP.NET Core).

## Projects not covered by an area doc

- **FocusTemplate.Admin.Client**, **FocusTemplate.Public.Client**: the generated typed HTTP clients of each vertical,
  referenced by every consumer, so the client an app ships is the one the tests drive.
- **FocusTemplate.Admin.Shared**, **FocusTemplate.Public.Shared**: each vertical's wire contract, generated records plus
  hand-written partials for computed members. They never reference each other.
- **FocusTemplate.Public.Mobile**: native MAUI with XAML, `net11.0-android;net11.0-ios` only (no Windows or
  MacCatalyst targets by decision, and iOS builds only on macOS), see [mobile](mobile.md).

## Tests

| Project | What it runs on |
|---|---|
| `FocusTemplate.Admin.Api.IntegrationTests` | `Microsoft.AspNetCore.Mvc.Testing`, Testcontainers Postgres, Respawn, `FakeTimeProvider`, see [integration tests](integration-tests.md) |
| `FocusTemplate.Admin.Web.E2E` | `Aspire.Hosting.Testing` booting the AppHost, Playwright, see [web E2E](web-e2e.md) |
| `FocusTemplate.Admin.Web.UnitTests` | plain xUnit, components rendered with `HtmlRenderer`, see [web forms](web-forms.md) |
| `FocusTemplate.ArchitectureTests` | reflection over the API assemblies and the EF model, see [database](database.md) |
| `FocusTemplate.Public.Mobile.E2E` | Appium with UiAutomator2 on the Android emulator, see [mobile](mobile.md) |

All use xUnit v3 on Microsoft.Testing.Platform.
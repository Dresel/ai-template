# FocusTemplate

An AI-first .NET 11 template on Aspire, split into two audience verticals: **Admin** (a Blazor
WebAssembly client served through a thin BFF over an internal API) and **Public** (a deliberately
exposed API + a native .NET MAUI mobile client). End-to-end OpenTelemetry, Umami analytics, and
Playwright E2E tests. Both APIs are **spec-first**: one `api.tsp` per vertical (`src/<vertical>/spec/`) generates the endpoints,
Mediator request records, wire models, typed clients and the OpenAPI document; every consuming project generates
its own slice into its `generated/` folder.

## Projects

Shared spine:

- **FocusTemplate.AppHost** - Aspire orchestrator. Wires both APIs, the BFF, an optional nginx TLS
  ingress, optional Umami analytics, and (behind `Features:Mobile`) the MAUI device resources + Dev Tunnel.
- **FocusTemplate.Primitives** - the typed ids every vertical shares, generated from `src/spec/primitives.tsp`
  (`output-type: primitives`) as Vogen value objects in namespace `FocusTemplate.Primitives`. The only project running
  Vogen's generator; `Data` and both `Shared` projects reference it, so one `WeatherForecastId` serves the domain and
  every contract.
- **FocusTemplate.Data** - the entities and their EF Core mapping: `Entities/` (each table's entity next to its
  `IEntityTypeConfiguration`, the configurations listed by hand in `ApplyEntityConfigurations`), `Auditing/`
  (`IAuditable`, `ICurrentUser`, the interceptor and the shadow columns), the model in `AppDbContextBase` with its two
  sealed contexts (`AppDbContext` for commands, migrations and the seed; `ReadOnlyAppDbContext` for queries, see
  **Read/write split**), the `Migrations/` folder, a design-time factory, and the dev seed. Referenced by both APIs and
  the AppHost migration resource. **No DDD layer**:
  entities are plain classes
  (`required`/`init`, public setters, no base types, no domain events) referencing each other by id without
  navigations. Business rules - lifecycle, ownership, thresholds - live in the Mediator handler that needs them, which
  answers a refusal with a case of its result union (see **Errors are values**). Postgres is the `postgis/postgis`
  image: the station location is a `geography` point via NetTopologySuite, created as
  `new Point(longitude, latitude) { SRID = 4326 }` (longitude first: swapped arguments still compile);
  `StationStatus` and `AlertKind` are native Postgres enums (`MapEnum` in the provider setup shared by
  `ConfigureAppDbContext` and `ConfigureReadOnlyAppDbContext`, which also switches on the naming and check-constraint
  plugins; every context gets its provider through them, never from a bare `UseNpgsql`). Admin registers both pools,
  `AddAppDbContextPool("focusdb")` and `AddReadOnlyAppDbContextPool("focusdb-readonly")`, Public only the read-only
  one, each followed by Aspire's `EnrichNpgsqlDbContext<TContext>()`; Data itself stays free of Aspire.
- **FocusTemplate.ServiceDefaults** - server-side Aspire defaults (OTel, service discovery, health).
- **`@spatialfocus/typespec-http-csharp-slim`** (npm, consumed as the packed tgz under `.npm/`; own repository
  `typespec-http-csharp-slim`, where it is documented and tested) - the TypeSpec emitter both verticals are generated
  with, and the TypeSpec library (`namespace SpatialFocus.Http`) every contract imports. The npm toolchain
  (`package.json`, `scripts/gen.mjs`) sits at the repository root; `npm run gen` runs one `tsp compile` per project
  `tspconfig.yaml`, and the parent emitter's scaffolding lands in the disposable `tsp-output/`. How this repository uses
  it is under **Spec-first APIs**; this repository only checks that regeneration is clean.

Admin vertical (`src/admin/`):

- **FocusTemplate.Admin.Api** - internal minimal API; never exposed to the browser directly. Endpoints, request
  records and result unions are generated into its `generated/<Slice>/` folder from `../spec/api.tsp` (namespace
  `FocusTemplate.Admin.Api.Features.<Slice>`, the one the slice's handlers use), the emitted `openapi.yaml` is
  served as-is; `Features/<Slice>/` holds the slice's Mediator handlers (EF Core, queries through `ReadOnlyAppDbContext`
  and commands through `AppDbContext`, backed by PostgreSQL) and endpoint hooks.
- **FocusTemplate.Admin.Client** - the generated typed HTTP clients of the vertical (`tspconfig.yaml`,
  `output-type: client`), referenced by every consumer so the client an app ships is the one the tests drive.
- **FocusTemplate.Admin.Web** - the Blazor WASM client (Blazorise Material UI). Talks to the API through the typed
  `WeatherForecastsClient` from `FocusTemplate.Admin.Client`.
- **FocusTemplate.Admin.Web.Bff** - thin YARP BFF: serves the WASM app and proxies `/_api/*` → API,
  `/_otlp/*` → dashboard, `/_analytics/*` → Umami. The browser only ever talks to the BFF.
- **FocusTemplate.Admin.Web.ClientServiceDefaults** - client-side OTel and shared WASM extensions.
- **FocusTemplate.Admin.Shared** - the wire contract shared between the server and the WASM client:
  records generated from the spec plus hand-written partials for computed members.

Public vertical (`src/public/`):

- **FocusTemplate.Public.Api** - the exposed API for external clients (mobile); generated from
  `src/public/spec/api.tsp` the same way, read-only EF Core access to the shared domain through
  `ReadOnlyAppDbContext`, its own audience-shaped contract.
- **FocusTemplate.Public.Client** - the generated typed HTTP clients of the vertical (`tspconfig.yaml`,
  `output-type: client`), referenced by every consumer.
- **FocusTemplate.Public.Mobile** - native .NET MAUI app (XAML), `net11.0-android;net11.0-ios` only
  (no Windows/MacCatalyst targets by decision; iOS builds only on macOS/CI). Calls the Public API through the
  `WeatherForecastsClient` from `FocusTemplate.Public.Client`.
- **FocusTemplate.Public.Mobile.ServiceDefaults** - MAUI counterpart of ServiceDefaults (service
  discovery, resilience, OTel) from the `maui-aspire-servicedefaults` template; no ASP.NET Core dependency.
- **FocusTemplate.Public.Shared** - the mobile wire contract. The two `Shared` projects must never
  reference each other.

## Tech stack

.NET 11 (preview) Packages use Central Package Management - see `Directory.Packages.props` for versions.

- **Orchestration (Aspire)** - `Aspire.AppHost.Sdk`; `Aspire.Hosting.Blazor` (WASM hosted-model
  service/telemetry proxying, experimental preview); `CommunityToolkit.Aspire.Hosting.Umami`
  (Umami analytics container + its Postgres backend).
- **BFF / gateway** - `Yarp.ReverseProxy`; `Microsoft.Extensions.ServiceDiscovery.Yarp` (resolves
  proxy destinations); `Microsoft.AspNetCore.Components.WebAssembly.Server` (serves the WASM client).
- **Blazor WASM client** - `Microsoft.AspNetCore.Components.WebAssembly`; `Blazorise.Material` +
  `Blazorise.Icons.Material` (Material 3 UI).
- **API contracts (TypeSpec)** - root `package.json`, pinned: `@typespec/compiler`, `@typespec/http`,
  `@typespec/openapi3` (emits OpenAPI 3.2) and our emitter `@spatialfocus/typespec-http-csharp-slim` as a
  `file:` dependency on `.npm/<name>-<version>.tgz` (bump = replace the tgz, `npm install`, `npm run gen`). The
  emitter wraps `@typespec/http-client-csharp` (**alpha**, daily builds) and ships its .NET plugin inside the package,
  so nothing in this solution compiles against those assemblies. The OpenAPI document is emitted from the spec and served as a static file, not reflected at
  runtime. **Mediator** - `Mediator.Abstractions` + `Mediator.SourceGenerator` (martinothamar, source-generated,
  MIT): generated endpoints dispatch `IQuery<T>`/`ICommand<T>` records to hand-written handlers.
- **Typed ids** - `Vogen` (source generator + `Vogen.SharedTypes` at run time). The emitter writes
  `[ValueObject<T>] [Instance("Unspecified", …)] public readonly partial struct` per `@typedId` scalar into
  `FocusTemplate.Primitives`; `Data` carries the `[EfCoreConverter<T>]` marker for the EF Core converters.
- **Data / EF Core** - `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` (client integration:
  `EnrichNpgsqlDbContext` adds retries, health checks and OTel to the pooled contexts `AddAppDbContextPool` and
  `AddReadOnlyAppDbContextPool` register);
  `Npgsql.EntityFrameworkCore.PostgreSQL` provider; `EFCore.CheckConstraints`
  (validation attributes such as `[Range]` become CHECK constraints); `EFCore.NamingConventions` (snake_case names in
  the database);
  `Microsoft.EntityFrameworkCore.Design` (design-time, tooling); the whole EF stack is pinned to one
  version in `Directory.Packages.props`. The `dotnet-ef` CLI is a repo-local tool
  (`.config/dotnet-tools.json`). AppHost wiring uses `Aspire.Hosting.PostgreSQL` +
  `Aspire.Hosting.EntityFrameworkCore` (`AddEFMigrations`).
- **Testing (data)** - `Testcontainers.PostgreSql` - real Postgres per integration-test assembly, the `postgis/postgis:17-3.5`
  image like the AppHost.
- **Shared infra (ServiceDefaults)** - `Microsoft.Extensions.ServiceDiscovery`;
  `Microsoft.Extensions.Http.Resilience` (Polly-based).
- **Observability** - `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`,
  and instrumentation for AspNetCore / Http / Runtime.
- **Testing** - `Aspire.Hosting.Testing`; `Microsoft.AspNetCore.Mvc.Testing`; `xunit.v3`;
  `Microsoft.Playwright.Xunit.v3` (E2E); `Microsoft.NET.Test.Sdk`.
- **Mobile (MAUI)** - `Aspire.Hosting.Maui` (**preview**, lockstep with the AppHost SDK) +
  `Aspire.Hosting.DevTunnels` for AppHost wiring; `Microsoft.Maui.Controls` version comes from the
  installed MAUI workload via `VersionOverride="$(MauiVersion)"`; `Microsoft.Maui.Core` is pinned in
  `Directory.Packages.props` and **must match the workload band** (`dotnet workload list`). Building
  the solution requires the `maui-android`/`maui-ios` workloads and the Android SDK platform matching
  the band (`dotnet build -t:InstallAndroidDependencies` installs it).
- **Analyzers** - `StyleCop.Analyzers` (global, enforced as build errors).

## Running

Use the Aspire CLI, not `dotnet run`:

- `aspire start` (detaches)
- `aspire stop`
- `aspire wait <resource>` - block until healthy

**Stop the AppHost before `dotnet build`/`dotnet test`** - a running BFF locks its output binary and
the build fails (MSB3027). To apply code changes without a full restart, use the **rebuild** command
on the affected resource (Aspire dashboard or MCP).

## Build, test, format

- `npm run gen` (repository root, after a one-time `npm ci`) - one `tsp compile` per `tspconfig.yaml` under
  `src/<vertical>/<project>/` and `tests/<project>/`, against `src/<vertical>/spec/api.tsp` (the vertical is the second
  segment of the project name, `FocusTemplate.Admin.Api` → `admin`). Run it after **any** change under `src/*/spec/` and
  after bumping the emitter tgz; the `generated/` folders and `openapi.yaml` are checked in and belong in the same commit.
  The shared spine project `FocusTemplate.Primitives` compiles `src/spec/primitives.tsp` in the same run (a project directly
  under `src/` compiles `src/spec/<name>.tsp`). Never edit generated files by hand; `tsp-output/` is disposable.
- `dotnet build FocusTemplate.slnx`
- `dotnet test FocusTemplate.slnx` - xUnit integration tests + Playwright E2E through the BFF
- `dotnet format <project>` - analyzers are strict (StyleCop + IDE rules as errors). Files written
  by tooling usually need this to fix line endings (CRLF, no final newline).
  **Never run `dotnet format` on the multi-targeted MAUI project** (`FocusTemplate.Public.Mobile`) -
  it processes each TFM as a separate project and writes conflict markers into shared source files.
  Fix its files by hand or with `dotnet jb cleanupcode --include=`.
- `dotnet jb cleanupcode FocusTemplate.slnx --profile="Built-in: Reformat Code" --include=<path>` -
  ReSharper formatting (repo-local tool, `dotnet tool restore` after a fresh clone; honors
  `.editorconfig` + `FocusTemplate.sln.DotSettings`). Prefer scoping with `--include` - a bare run
  reformats the whole solution.

## Agent toolbox

**Aspire moves fast - always check the current Aspire docs before designing, recommending, or
changing anything Aspire-related** (AppHost wiring, integrations, migrations, publish/deploy):
`aspire docs search` or MCP `search_docs`. Built-in model knowledge is stale - e.g. the hand-rolled
EF migration-worker pattern was superseded by `AddEFMigrations`.

Skills live in `.claude/skills/`. Pick by task - these are all permission-allowlisted:

| Need | Use |
|------|-----|
| Operate the app: start/stop/wait, resource status | `aspire` skill → Aspire CLI (`aspire start/stop/wait/describe`) |
| Runtime evidence from a running app: logs, traces, resources | Aspire MCP (`list_resources`, `list_console_logs`, `list_structured_logs`, `list_traces`) |
| Apply a code change to a running resource | Aspire MCP `execute_resource_command` → `rebuild` (no full restart) |
| Aspire API / workflow questions | `aspire docs search/get`, `aspire docs api search --language csharp` (or MCP `search_docs`/`get_doc`) |
| Any .NET package API question (Blazorise, YARP, OTel, …) | `dotnet-inspect` skill: `dnx dotnet-inspect -y -- member/type/find/diff --package <id>` |
| Browser reproduction, manual UI checks, screenshots | `playwright-cli` skill (persistent E2E tests go in `FocusTemplate.Admin.Web.E2E`) |
| Drive the app on the Android emulator: find/tap/type/screenshot/page source | `appium` MCP (element-based, same locator semantics as the tests; persistent E2E tests go in `FocusTemplate.Public.Mobile.E2E`). Raw `adb` is the fallback + logcat channel |
| Change an API (route, wire model, status code, new operation) | edit `src/<vertical>/spec/<Slice>.tsp` (new slice: add the file + import it in `spec/api.tsp`) → `npm run gen` (repository root) → implement/adjust the Mediator handler in the Api project's `Features/<Slice>/` → fix the consumers, which compile against the regenerated `{Interface}Client` (never hand-write HTTP calls in Web/Mobile) → tests. This repository's rules: **Spec-first APIs** under Conventions; what the emitter produces is documented with the emitter |
| Formatting | `dotnet format`, `dotnet jb cleanupcode` (see above) |
| Wire an existing app into Aspire (one-time) | `aspireify` skill - already completed for this repo |

## Development loop

Test-first. Don't change production code first - for a bug, reproduce it with a failing test; for a
feature, specify the new behavior with one.

1. **Triage** - state observed vs. expected behavior, affected area, repro steps, and the test level
   (below). No production code yet.
2. **Read first** - Grep/Glob/Read the affected code and its existing tests. Don't guess APIs:
   Aspire questions → `aspire docs`; any other package (Blazorise, YARP, OTel, …) → `dotnet-inspect`.
3. **Add the failing test at the smallest level that fits:**
   - **Integration** (one service: DI, middleware, serialization, auth, framework, **EF Core / SQL**) →
     `FocusTemplate.Admin.Api.IntegrationTests`. A real Postgres runs via Testcontainers (assembly fixture,
     migrations applied once); tests arrange rows against an empty schema. See **Integration test
     database** below for the reset/isolation contract.
   - **Aspire system** (cross-resource: BFF↔API, ingress, service discovery, startup order,
     scale-out, telemetry) → `FocusTemplate.Admin.Web.E2E` (boots the AppHost via
     `DistributedApplicationTestingBuilder`).
   - **UI** (DOM, input, caret, focus, keyboard, routing, client validation, user flow) →
     `FocusTemplate.Admin.Web.E2E` Playwright, through the BFF.
   - **UI (mobile)** (native MAUI flows on the Android emulator) → `FocusTemplate.Public.Mobile.E2E`
     Appium/UiAutomator2; boots the AppHost, installs the APK with a baked test env (`adb reverse`,
     no Dev Tunnel). Appium + driver are **project-local npm devDependencies** — one-time `npm ci`
     in the test project; auto-skips without that or an emulator. Locators: MAUI `AutomationId` =
     Android `resource-id` → `MobileBy.Id("<AutomationId>")` (driver auto-prefixes the app package;
     `AccessibilityId` does not match).
   - **Convention** (a rule every handler or type of both APIs must follow, checked by reflection over the API
     assemblies) → `FocusTemplate.ArchitectureTests`; today the read/write context rule.
   - **Unit** (pure logic) → add a unit project when such logic first appears; none today (the emitter's tests live in
     the `typespec-http-csharp-slim` repository).
4. **Watch it fail** - stop the AppHost (bin lock), then `dotnet test FocusTemplate.slnx --filter <name>`.
   Confirm the failure matches the report, not a setup gap.
5. **Smallest fix** - minimal production change to green the test; `dotnet format <project>` new files.
6. **Re-run** the new test, then the affected area's existing tests.
7. **Summarize** - root cause, test added/changed, fix, commands run, remaining risks. Commit only when asked.

**Runtime / distributed bugs → drive through Aspire**, don't hand-start processes: `aspire start` /
`aspire wait <resource>`; read evidence via the Aspire MCP (`list_resources`, `list_console_logs`,
`list_structured_logs`, `list_traces`); `rebuild`/`restart` a resource in place to apply a fix
without a full restart.

**UI bugs → the `playwright-cli` skill:** real browser + keyboard, never value assignment. Input/caret
bugs: navigate → locate by `data-testid` → set value → set `selectionStart`/`selectionEnd` → type via
key events → assert value *and* caret.

**Mobile bugs/features → red-first in `FocusTemplate.Public.Mobile.E2E`.** Interactive driving via
the `appium` MCP (find element / tap / type / screenshot / page source) - same locator semantics as
the tests (`MobileBy.Id("<AutomationId>")`), so an interactive repro translates 1:1 into the failing
test, which is written **before** the fix. New UI: decide the `AutomationId`s up front - they are
the spec the red test asserts against; implement, then re-run (the fixture rebuilds + reinstalls the
APK every run, so there is no stale-APK risk in the test path). Pure visuals (layout, theming):
verify by emulator screenshot, no persistent test. Raw `adb` is the fallback + evidence channel:
`adb exec-out screencap -p`, `adb shell uiautomator dump`, `adb logcat` (tag `template.mobile`).
Needs a running emulator; boot with `emulator -avd <name>` and wait with
`adb wait-for-device shell 'while [ "$(getprop sys.boot_completed)" != "1" ]; do sleep 2; done'` -
the loop runs device-side, so the host command stays a single allowlisted `adb` invocation (a
host-side shell loop around `adb` triggers a permission prompt).

**Scale-out bugs:** first a deterministic integration test with two BFF instances sharing one session
(req 1 → instance A, req 2 → instance B; assert business behavior, not infra). Escalate to an Aspire
system test through the nginx ingress only if real infra is required, using logs/traces to find the
failing resource.

**Avoid permission prompts - prefer allowlisted MCP tools and bare single commands.** The
`.claude/settings.json` allowlist matches a compound command **segment by segment**: a pipeline
(`a | b | c`) auto-runs when **every** segment matches an allow rule **and none matches a deny rule**
(deny always beats allow). So `dotnet build * | Select-String * | Select-Object *` auto-runs once each
segment is allowed - the `Select-String`/`Select-Object`/`ConvertFrom-Json` allow entries exist for
exactly this. Traps, learned the hard way:
- **Workspace trust gates the project allowlist.** In an untrusted workspace every `allow` rule in
  `.claude/settings.json` is silently ignored, while read-only commands still auto-run via built-in
  heuristics - which masks the problem. Symptom: allowlisted *mutating* commands (build/format/test)
  prompt although read-only ones don't. Trust is per-user state in `~/.claude.json`; on Windows the
  same folder can be registered twice (`C:\…` vs `C:/…` spelling, e.g. CLI vs desktop app) with
  separate trust flags - check `hasTrustDialogAccepted` on **both** entries.
- Settings changes load at **session start** - restart the session after editing any settings file.

## Conventions

- **Comments** explain *why*, never *what*: @.agents/comments-aspire.md.
  Two rules on top, specific to this repo. **No XML doc comments (`///`) on hand-written C#** - the
  signature carries the *what*; the exceptions are TypeSpec `/** */` (see **Spec-first APIs**) and
  hand-written partials on generated wire/client types, whose XML reaches consumers. **Nothing under
  `generated/` or `Migrations/` is commented** - it is overwritten. A comment describes the code as it
  stands, never the process that produced it ("as discussed", "per review", "was previously X"); links
  to *upstream* issues beside a workaround are wanted.
- **AOT-ready**: nothing publishes NativeAOT yet - EF's support is
  [experimental](https://learn.microsoft.com/en-us/ef/core/performance/nativeaot-and-precompiled-queries) - but moving
  to it once EF ships compiled models and precompiled queries as stable must stay a publish setting, not a rewrite. So
  no reflection-based discovery: register by hand (a new `IEntityTypeConfiguration<T>` goes into
  `ApplyEntityConfigurations`, a new service into its feature's extension method), treat an API marked
  `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]` as a stop sign, and prefer source generators (Vogen, Mediator
  and the System.Text.Json contexts already are). Write each EF query as one method-syntax chain from the `DbSet` to its
  terminal operator - the precompiler cannot follow a query composed across statements or written in query syntax -
  and keep value converters free of captured state.
- **Extension classes** are named after the type they extend, without an interface's `I`: `ModelBuilderExtensions`,
  `ServiceCollectionExtensions`, `ProjectResourceBuilderExtensions` for `IResourceBuilder<ProjectResource>`, and after
  the constraint for a generic receiver (`TBuilder : IHostApplicationBuilder` → `HostApplicationBuilderExtensions`).
  A shortened name is fine where it stays unambiguous (`BuilderExtensions` in the Blazor app), but not in a shared
  namespace such as `Microsoft.Extensions.Hosting`. Classes that map endpoints (`app.MapGet(...)`) are named after their
  endpoints instead (`DebugEndpoint`). One class per extended type and namespace: the namespace says what the methods
  are about (`Data.Auditing`), the method name what they do (`AddAuditingShadowProperties`). Names that came with a
  template stay, so the file still diffs against a newer template: `Extensions` in both ServiceDefaults projects,
  `BlazorClientExtensions`.
- **Feature flags**: `Features:Analytics`, `Features:TlsOffloadingIngress`, and `Features:Mobile`
  (default **off**: no devtunnel/emulator requirements on a plain `aspire start`) in the AppHost's
  `appsettings.json`, overridable per-developer via the gitignored `appsettings.local.json`. The
  E2E fixture pins them via CLI args.
- **Shared DTOs** live in the vertical's `Shared` project (`FocusTemplate.Admin.Shared` /
  `FocusTemplate.Public.Shared` - never referencing each other); computed members go into hand-written
  partials next to the project file. Entities (`FocusTemplate.Data`) stay server-side; map entity =>
  DTO in the Mediator handler, never expose entities to the client.
- **Read/write split**: query handlers (`IQueryHandler`) take `ReadOnlyAppDbContext`, command handlers
  (`ICommandHandler`) take `AppDbContext`, never the other way round - a command reading through the second context
  would lose its transaction and its tracking. `FocusTemplate.ArchitectureTests` checks the constructor parameters of
  every handler in both APIs, and forbids the shared `AppDbContextBase` for both kinds, so a handler always names the
  context it means. The read-only context tracks nothing, refuses `SaveChanges`, and is registered on its own
  connection string key `focusdb-readonly`, meant for the SELECT-only `focusdb_reader` role (see **Reader role**); the
  role, not the C# guard, is what stops `ExecuteUpdate`/`ExecuteDelete` and raw SQL. Locally the AppHost injects the
  owner connection under both keys, so a plain `aspire start` needs no second login; the integration tests run the real
  role, and a published environment points the second key at it.
- **Keep `data-testid` attributes** - the Playwright E2E suite selects on them. The MAUI equivalent
  is **`AutomationId` on every interactive control** (Appium selects on it).
- **Mobile dev loop**: enable `Features:Mobile` in `appsettings.local.json`; first start prompts to
  install/login the `devtunnel` CLI (the Dev Tunnel exposes `public-api` to the emulator; anonymous,
  dev-only). Android needs a running emulator (`adb devices`); the iOS simulator resource shows
  "unsupported" on Windows - it runs from a macOS host only.

### Spec-first APIs

Both APIs are generated from TypeSpec by `@spatialfocus/typespec-http-csharp-slim`.

- **Layout**: one contract per vertical in `src/<vertical>/spec/`. `api.tsp` carries the service
  metadata and imports one `<Slice>.tsp` per feature slice, all sharing the namespace. Three projects per
  vertical generate, each with its own `tspconfig.yaml` and `generated/` folder: the Api project
  (`output-type: api`), the Shared project (`contracts`) and the Client project (`client`). Everything
  else consumes them by project reference, so a new consumer references `FocusTemplate.<V>.Client`
  rather than generating a copy of it, and generated files are never linked across projects. The typed ids live in
  `src/spec/primitives.tsp` (namespace `FocusTemplate.Primitives`, no service), imported by every `api.tsp`;
  `FocusTemplate.Primitives` generates them with `output-type: primitives`, the verticals reference them by namespace.
- **Namespaces**: `api-namespace: FocusTemplate.<V>.Api.Features.{interface}` puts a slice's
  generated code in the same namespace as its hand-written handlers, and `client-namespace:
  FocusTemplate.<V>.Client.{interface}` does the same on the consumer side. What every slice shares
  (`NotFound`, `ApiClientSupport`, the `{Status}Problem` cases) lands one namespace up.
- **Hand-written code per slice** lives in `src/<vertical>/FocusTemplate.<V>.Api/Features/<Slice>/`:
  the Mediator handlers, and `*Endpoints.Hooks.cs` implementing the `ConfigureGroup` /
  `Configure{Op}` hooks for auth, rate limiting and caching. Consumers call the generated client and
  never hand-write HTTP.
- **Errors are values**: a handler returns the generated result union, answering a modeled status
  with its case record (`new NotFound("…")`) and never with an exception. Anything genuinely
  unhandled becomes a 500 problem response through `AddProblemDetails()`. Consumers match the
  client-side union exhaustively, so a status added to the spec is a compile error everywhere it is
  not handled yet.
- **Client registration**: reference `FocusTemplate.<V>.Client` and register the generated client with the
  existing typed-HttpClient helpers,
  whose `BaseAddress` carries the BFF prefix or the service-discovery name. Extra members go into a
  hand-written partial next to the consumer's project file.
- **Spec style**: routes are kebab-case plural nouns (`/weather-forecasts`). DTOs carry a `Request`
  or `Response` suffix, which also keeps them distinct from the like-named entities in
  `FocusTemplate.Data` that the handlers map from. DELETE of a missing resource answers 204, so a
  retrying client stays idempotent. JSON stays camelCase, which is TypeSpec's property style and
  ASP.NET Core's web default rather than a setting anyone chose.
- **Doc comments** (`/** */`) are published to OpenAPI and generated XML summaries, so write them for API
  callers. Use `//` comments for spec rationale, including emitter workarounds; these stay in source.
- **Emitter traps worth repeating here**, because they fail silently rather than at build time:
  identifiers use the shared `uuid` scalar, never
  `@format("uuid")` on a string; error responses are `Problem<Status>`, never `@error` models; and
  TypeSpec defaults on optional parameters never reach the server, so the handler applies them; and a `@typedId` scalar
  declared inside a service namespace is an emitter error, typed ids belong in `primitives.tsp`.
- **Workflow**: change a slice file, run `npm run gen` at the repository root, adjust the handler,
  then fix the consumers, which stop compiling exactly where the contract moved. Never edit a file
  under `generated/`. The `generated/` folders and `openapi.yaml` are checked in and belong in the
  same commit as the spec change.

### Database & migrations

- **Mapping**: a property's value constraints are annotations on the entity - `[MaxLength]`, `[Precision]`, `[Range]` -
  so the limit sits where the property is declared. Every string property has a `[MaxLength]`, so no column ends up
  unbounded `text` by default. The entity's `IEntityTypeConfiguration` keeps what is not a constraint on a single
  value: keys and sentinels, column types, relationships without navigations, and indexes.
  Validation attributes EF ignores (`[Range]`, `[MinLength]`, `[RegularExpression]`, ...) become CHECK constraints
  through `EFCore.CheckConstraints` - but it silently skips a `[Range]` whose bounds are not of the property's type,
  and `RangeAttribute` has no decimal bounds, so a decimal range is a `HasCheckConstraint` in the configuration.
- **Naming**: the database uses PostgreSQL's snake_case (`weather_forecasts.temperature_c`, `pk_`/`fk_`/`ix_` keys and
  indexes) through `EFCore.NamingConventions`, while C# keeps its own names. Raw SQL and `HasCheckConstraint` bodies
  use the database names; `__EFMigrationsHistory` keeps its name.
- **Auditing**: `IAuditable` entities get four shadow columns (`CreatedAt/By`, `UpdatedAt/By` as `UserId`) from
  `AddAuditingShadowProperties`, set by `AuditingInterceptor` (`TimeProvider` + `ICurrentUser`), which
  `ConfigureAppDbContext` attaches to the write context. `AddAppDbContextPool` resolves it from the container, so each
  host that registers the write pool registers both inputs; a host that only queries (the Public API today) registers
  `AddReadOnlyAppDbContextPool` alone and needs neither. `ExecuteUpdate` and raw SQL bypass it:
  on `IAuditable` types set the audit columns explicitly or use `SaveChanges`. `ICurrentUser` is the fixed
  `WellKnownUsers.Developer` until authentication lands, `WellKnownUsers.System` where no person acts (the dev seed,
  jobs);
  tests use `ApiFixture.TestUser` and `ApiFixture.Clock`, a `FakeTimeProvider` they advance instead of assuming a time.
  Tests arrange rows through
  `Factory.CreateDbContext()`, which carries the provider options and the interceptor like the API does.
- **Typed ids as keys**: `VogenEfCoreConverters` in `Data` carries one `[EfCoreConverter<T>]` per id and
  `ConfigureConventions` calls the generated `RegisterAllInVogenEfCoreConverters()`. A store-generated key needs the
  sentinel: the entity initializes it with `Id.Unspecified`, the model declares `ValueGeneratedOnAdd().HasSentinel(…)`,
  since the integer-key convention does not reach a key behind a converter and EF reads the key before generating one.
  Without `HasSentinel` the zero is written into the identity column and the second insert collides.
- **Schema is applied by the `migrations` resource, never by the API.** The API only reads/writes;
  it `WaitForCompletion`s the migration resource. This is safe under scale-out (no startup migration
  race). Locally/E2E the resource runs `dotnet ef database update` on start; `aspire publish` emits it
  as an idempotent migration-bundle container (a one-shot Job/`restart:no` per compute target).
- **Reader role**: `focusdb_reader` belongs to the environment, not to the schema. No migration creates it or grants to
  it: roles are cluster-level, `CREATE ROLE` needs `CREATEROLE`, which the identity running the bundle usually lacks,
  and default privileges set by a migration would bind to whichever role ran it. Every environment that uses the
  read-only key creates the login (or grants the role to its own identity) and, once the schema is applied, runs
  `GRANT USAGE ON SCHEMA public TO focusdb_reader; GRANT SELECT ON ALL TABLES IN SCHEMA public TO focusdb_reader;`.
  Tests: `PostgresFixture` creates the login once per container, `ApiFixture` runs the grants on its class database
  right after migrating it. Production: infrastructure provides both, and repeats the grant after a bundle that adds
  tables, or sets `ALTER DEFAULT PRIVILEGES FOR ROLE <migrating role> IN SCHEMA public GRANT SELECT ON TABLES TO
  focusdb_reader` once. Dev: nothing, no login exists and the AppHost hands the owner connection to both keys.
- **Add a migration** (stop the AppHost first - bin lock):
  `dotnet dotnet-ef migrations add <Name> --project src/FocusTemplate.Data --startup-project src/FocusTemplate.Data --context AppDbContext`.
  `--context` is required because the assembly holds two contexts and only `AppDbContext` has migrations (the
  `migrations` resource names it too). The design-time `AppDbContextFactory` needs no live DB for this. Migration files land in
  `src/FocusTemplate.Data/Migrations/` and are exempt from StyleCop via an `.editorconfig`
  `generated_code` carve-out. You can also use the migration resource's dashboard commands
  (Add Migration, Update/Reset/Drop Database, Status).
- **Dev seed data** lives in `WeatherSeed` and runs via EF `UseSeeding`/`UseAsyncSeeding` when the
  migration tool applies migrations in **run mode only** (the AppHost sets `Database__SeedTestData` on
  the tool resource via `configureToolResource`; the published bundle never seeds). The seed is
  idempotent (insert-if-empty). Implement **both** the sync and async seed delegates - the EF CLI uses
  the synchronous one. Integration tests deliberately run against an unseeded schema.
- **No data volume** on the dev Postgres: each `aspire start` / E2E run gets a fresh, re-seeded
  database, keeping runs deterministic and hermetic.

### Integration test database

Isolation model: **one container, one database per test class, fresh state per test.**

- `PostgresFixture` (assembly fixture) starts **one** Postgres container, creates the `focusdb_reader` login once
  (cluster-level; the grants are per database and belong to `ApiFixture`), and hands out databases on
  demand (`CreateDatabaseAsync`). Only `CREATE DATABASE` is serialized (concurrent creations contend
  on the template database); migrations run in parallel.
- `ApiFixture` (class fixture - xunit creates one instance **per test class**) provisions its own
  GUID-named database, migrates it, grants `focusdb_reader` `SELECT` on it, and points the API at it under both keys:
  `focusdb` as the owner, `focusdb-readonly` as `focusdb_reader`, so the API's queries run on the real role.
  `Factory.CreateDbContext()` stays on the owner. Test classes therefore share nothing and run **in parallel** - no
  xUnit collection needed.
- **Test classes derive from `ApiTestBase`**: before every test it resets the class database via
  `ApiFixture.ResetAsync()` ([Respawn](https://github.com/jbogard/Respawn), FK-safe,
  `__EFMigrationsHistory` preserved so migrations never re-run). Every test starts on an empty,
  migrated schema and **arranges exactly the rows it asserts** (`Factory.CreateDbContext()`).

Tests never rely on the dev seed - `WeatherSeed` is dev/E2E-only, and the E2E suite verifies it
through the production seeding path. If a read-heavy suite over an expensive shared dataset emerges
later, add a **seeded, immutable, shared** database + fixture for those tests (seed once, read in
parallel, never mutate).
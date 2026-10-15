# Feature packages: plan

*Written 2026-10-12, nothing started. Grew out of the Lamama plan (`lamama.md`): Releases should be a feature that
drops into a host, so the template has to learn feature packages first. Weather is the spike.*

## Goal

A feature is a set of projects under `src/features/<name>/` that a host adds with a few lines: project references,
`AddX()` and `MapXEndpoints()` in `Program.cs`, one migration resource in the AppHost, one import in `primitives.tsp`,
its configuration in `appsettings.json`. The spine (identity, auditing, realtime, the Web foundation) stays in the
template's projects. Weather is the first feature and the spike; Releases is the second, in Lamama.

## Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Identity | Stays in the spine. Every host needs it, nothing is gained by making it optional. |
| 2 | Feature data | A feature owns its entities, its context with a read-only twin, its schema, its migrations and its seed. Feature entities never go into `FocusTemplate.Data`. |
| 3 | FK to users | Through `UserReference` in `Data.Foundation`, mapped to `public.users` and excluded from feature migrations. The feature's migration creates the constraint. |
| 4 | Queries across the boundary | A feature's context knows its own tables plus the reference shapes the spine promises (`UserReference`, later the outbox). Between two features: ids plus a lookup, never a shared entity. |
| 5 | Schemas | `public` is the spine, a schema is a feature. Each schema holds its own `__EFMigrationsHistory`. Nothing existing moves. |
| 6 | Library types | The emitter writes the spec-independent types once into `FocusTemplate.Http` (`output-type: library`): `ProblemDetails`, the validation problem, `PermissionRequirement` with `RequiresPermissionAttribute`, `ApiFailure`, `ApiOutcome`, `IProblemOf`, `Success`, `ServerEventReader`. Every Admin and feature `tspconfig.yaml` sets `library-namespace: FocusTemplate.Http` and gets none of its own. `Primitives` keeps Vogen as its only package. |
| 7 | Permissions of a feature | Declared in the feature's spec folder, imported by the host's `primitives.tsp`. `Permission.All` and `HasData` stay as they are; adding a feature yields a spine migration for its rows, like adding a permission does today. |
| 8 | Ownership checks | Handler logic over `ICurrentUser`: owner, or a permission that overrides. `@requiresPermission` on the operation stays the coarse gate. |
| 9 | What a feature references | `Primitives`, `Http`, `Data.Foundation`, `Api.Foundation`, `Admin.Web.Foundation`. Never `Data`, `Admin.Api`, `Admin.Web`. An architecture test says so. |
| 10 | Context pooling | A pooled context's only constructor parameter is `DbContextOptions<T>`. The auditing interceptor and `ICurrentUser` stay singletons resolved at `SaveChanges`. An architecture test pins the constructor, a two-user audit test pins the behaviour for feature contexts. |

## Target layout

```
src/
  FocusTemplate.Primitives              typed ids, Permission, Permission.All (generated); Vogen only
  FocusTemplate.Http                    the library types of decision 6 (generated, output-type: library)
  FocusTemplate.Data.Foundation         context base, AuditingInterceptor, IAuditable, ICurrentUser, Vogen converters,
                                        SearchPattern, AddFeatureDbContextPool<T>, UserReference + configuration
  FocusTemplate.Data                    User, Group, …, AppDbContext, ReadOnlyAppDbContext, migrations, seeds
  FocusTemplate.Api.Foundation          Realtime/ (streams, backplane, AddSignal), the access token's claims
  admin/FocusTemplate.Admin.Web.Foundation
                                        Foundation/ (pages, forms, feedback, validation, shell pieces), PermissionView,
                                        PermissionClaimsHandler, CurrentUser, IPageLocalizations, _Imports.razor
  admin/FocusTemplate.Admin.Api         Program.cs, Authentication/, Authorization/ (handler, bootstrap), Chaos/,
                                        the host's own slices (user management, demo profiles)
  admin/FocusTemplate.Admin.Web         App.razor, MainLayout, Program.cs, the host's resx and slices
  features/weather/
    spec/                               weather.tsp, permissions.tsp
    FocusTemplate.Weather.Shared        wire contracts (generated)
    FocusTemplate.Weather.Client        typed clients (generated)
    FocusTemplate.Weather.Data          Station, Observation, Alert, WeatherForecast, WeatherDbContext and its
                                        read-only twin, schema weather, migrations, WeatherSeed, NetTopologySuite
    FocusTemplate.Weather.Api           handlers, endpoints (generated), AddWeather(), MapWeatherEndpoints()
    FocusTemplate.Weather.Web           Razor class library: the page, .razor.css, resx, AddWeatherPages()
```

References: the host references everything. A feature references the five spine projects of decision 9. Both verticals'
APIs reference `Weather.Data`; each keeps its own spec and handlers.

## Database conventions

- `public` holds the spine: identity today, infrastructure tables later (outbox, idempotency keys). A feature gets a
  schema named after itself with its own history table: `AddFeatureDbContextPool<TContext>(connectionName, schema)` sets
  the default schema and `MigrationsHistoryTable("__EFMigrationsHistory", schema)`.
- Postgres enums are schema-scoped: `HasPostgresEnum(schema, name)` plus the schema in `MapEnum`.
- PostGIS stays in `public`, where extensions live. The package moves to `Weather.Data`; the image stays the host's
  choice, documented as the feature's requirement.
- Migration order: the feature's migration resource waits for the spine's, because the FK needs `public.users`.
- Removing a feature from a database is `DROP SCHEMA <feature> CASCADE`.
- When the outbox comes, it's the second shared table the spine promises: `OutboxMessage` in `Data.Foundation`, mapped
  to `public.outbox_messages`, excluded from feature migrations, applied to every feature context, so the row is written
  in the feature's transaction and dispatched from the spine's.

## Spine changes

1. **`Data.Foundation`** split out of `Data`, in a namespace of its own (`FocusTemplate.Data.Foundation`). New: `AddFeatureDbContextPool<TContext>` generalizing
   today's pool registration (connection name, schema, the singleton interceptor); `UserReference { Id, DisplayName, IsActive }`
   with a configuration on `public.users`, `ExcludeFromMigrations()`, no `DbSet`.
2. **`Api.Foundation`** out of `Admin.Api`: `Realtime/` and the access token's claims. Localization is host composition and stays, the outcome helpers are generated per API.
   `Authentication/`, `Chaos/` and the permission handler stay, they're identity and host policy.
3. **`Admin.Web.Foundation`** out of `Admin.Web`: the `Foundation/` folder, `Infrastructure/Authorization`, `CurrentUser`,
   `IPageLocalizations`, with an `_Imports.razor` so feature pages get the same usings.
4. **`FocusTemplate.Http`**: generated from `src/spec/http.tsp` with `output-type: library`; the Admin projects reference it.
5. **Emitter**: `output-type: library` and the `library-namespace` option, which makes the contracts, api and client
   outputs reference the library types instead of writing them. Bump per `docs/spec-first.md`.
6. **`gen.mjs`**: learns `src/features/<name>/`; a feature's `Shared`, `Client` and `Api` each carry a `tspconfig.yaml`
   compiling `src/features/<name>/spec/`.
7. **Architecture tests**: the handler-context rule generic over any read-only context by base type; no feature assembly
   references `*.Data`, `*.Admin.Api`, `*.Admin.Web`; every pooled context has exactly one constructor taking only
   `DbContextOptions<T>`.
8. **Integration test fixture**: registers the feature contexts, applies spine migrations then feature migrations.
   `AuditingTests` and `ReadOnlyDbContextTests` switch to `Group` as their test entity.
9. **`primitives.tsp`** imports `src/features/weather/spec/permissions.tsp`.

## The weather feature

- Entities, configurations and enums move to `Weather.Data`; `WeatherDbContext` and `ReadOnlyWeatherDbContext` on
  `focusdb` and `focusdb-readonly`; one `InitialCreate` in schema `weather`; `WeatherSeed` registered by `AddWeather()`.
- `Station.Owner` navigation to `UserReference`, FK with `Restrict`; the list projects `Owner.DisplayName` into the wire model.
- `WeatherPermissions.ManageStations`; the update handler allows the owner or the permission, the list filters the
  same way.
- Handlers, generated endpoints and clients, the page with its resx move into the feature projects.
- Tests that move with it: the weather integration tests, the mobile smoke test's expectations. New: user A and user B
  save a station through the pooled `WeatherDbContext`, each row carries its own user; `UserReference`'s mapping matches
  `User`'s; the permission chain end to end (declared in the feature, seeded, grantable in the Groups page, enforced on
  the endpoint).

## Host composition

```csharp
// Admin.Api/Program.cs
builder.Services.AddWeather();
endpoints.MapWeatherEndpoints();

// AppHost
IResourceBuilder<EFMigrationResource> weatherMigrations = api.AddEFMigrations("weather-migrations", ...)
    .WithMigrationsProject("../features/weather/FocusTemplate.Weather.Data/FocusTemplate.Weather.Data.csproj")
    .WaitForCompletion(migrations);

// Admin.Web/Program.cs and App.razor
builder.Services.AddWeatherPages();   // the menu entry, the page's services
<Router AppAssembly="…" AdditionalAssemblies="[typeof(WeatherPage).Assembly]">
```

## Order of work

One commit each, so the foundations can stay if the packaging is dropped.

1. `Data.Foundation` split.
2. `Api.Foundation` split.
3. `Admin.Web.Foundation` split, with `_Imports.razor`.
4. Emitter library output and bump, `FocusTemplate.Http`, `gen.mjs` layout.
5. `Weather.Shared`, `.Client`, `.Api` generated and compiling, handlers moved. **Stop check**: Mediator finds the
   handlers in the referenced assembly.
6. `Weather.Data` with context, schema, history table, migration, seed, the `UserReference` FK; AppHost and fixture wiring.
7. Permission, owner check, name projection, the new tests; integration and architecture tests green.
8. `Weather.Web`; E2E green.
9. Docs: `architecture.md` (feature layout, reference rule), `database.md` (contexts per feature, schemas, `UserReference`,
   pooling invariant), `spec-first.md` (feature spec layout, emitter option), `authorization.md` (the project), the
   `feature-slice` skill, the AGENTS.md index.

Stop criteria, both visible by step 5: the emitter needs more than the one option, or Mediator doesn't see handlers
across assemblies without a workaround.

Estimate: four to five days.

## Afterwards

- **Lamama** forks the template, leaves weather out by not referencing it, adds `features/releases/` with
  `ReleaseManagement` and its simulation beside it. Products are a dimension inside the feature, configured by the host:
  Landmanager (Scaleway registry, maintenance step) and Lamama itself (ACR registry, no maintenance step), the latter
  being the first production use. `lamama.md` steps 1, 3 and 5 change accordingly.
- **The click dummy's CSS** splits: tokens, Radzen theme variables and the shell go to the host; component styles go to
  `.razor.css` files in `Releases.Web`; state semantics (sync, drift, maintenance, …) are custom properties in the
  feature's stylesheet with fallbacks, so the host's palette wins where there is one.
- **`<UserName Id="…" />`** in `Admin.Web.Foundation`, backed by an ids-to-names endpoint with a cache, the day a page
  shows its first audit column.
- **Per-feature permission constants** generated into the feature's `Shared`, if the import list in `primitives.tsp`
  ever bothers us.

## Risks

- **Scoped CSS and resx from a class library in the WASM bundle**: expected to work, checked in step 8.
- **Enum mapping in a schema**: fails at runtime if the schema is missing in one of the two places; covered by the
  weather integration tests.
- **Two migration resources racing**: prevented by `WaitForCompletion`; the separate history tables remove the shared
  table anyway.
- **Template drift for Lamama** grows by the spike's duration; the fork waits for it.

## Outcome (2026-10-12)

All nine steps done in one working tree, uncommitted. Unit 90, architecture 18, integration 78 and E2E 57 green. The mobile E2E suite needs the emulator and did not run.

Where it went differently than planned:

- **Namespaces**: `Data.Foundation` and `Api.Foundation` got namespaces of their own (`FocusTemplate.Data.Foundation`,
  `FocusTemplate.Api.Foundation`). Keeping `FocusTemplate.Data` across two assemblies collides on the extension class
  names the C# rule prescribes. `Admin.Web.Foundation` kept its namespaces, they already matched the folder.
- **`Api.Foundation`** holds `Realtime/` and the access token's claims only. Localization is host composition, and the
  outcome helpers are generated per API.
- **The emitter needed more than one option** (stop criterion 1). The Web foundation works with `ValidationProblemDetails`,
  `ApiFailure` and `ServerEventReader`, which every contracts and client project generated a copy of. The emitter got
  `output-type: library` and `library-namespace` (scenario `library-namespace`, 499 tests); `FocusTemplate.Http` holds
  the types once, replacing the hand-written `FocusTemplate.Authorization`. The Public vertical keeps its own copies.
- **Mediator** (stop criterion 2) scans referenced assemblies by default: the host finds `Weather.Api`'s handlers with
  nothing configured.
- **The permission claim name** moved from `BffClaimTypes.Permission` to `Permission.ClaimType` in `Primitives`, so
  `CurrentUser` and the claims handler moved into `Admin.Web.Foundation` without a host reference.
- **Weather's typed ids stay in `primitives.tsp`**: their hand-written partials (`StationId.New()`) sit in
  `Primitives`, and a feature-owned id would leave them orphaned in a host without the feature. Only the permission
  file is the feature's.
- **The spine's migrations were squashed** into one `InitialCreate`: the old ones created weather's tables in `public`
  and needed NetTopologySuite to compile. A persistent dev database is dropped once.
- **Stations page** added afterwards: `/weather/stations` lists the stations with their owners, and the owner, or whoever holds
  `Weather.ManageStations`, renames one in a dialog. The menu entries stay in the host's `MainLayout`.
- **`FoundationDbContext<TModel>`** added afterwards: it seals the conventions, the default schema and the audit
  columns for every context, and the schema is stated once, as the static `IHasSchema.Schema` the registration reads.
  The models did not change, so no migration followed.
- **Validation texts of a feature** (`WeatherTexts`) are not hooked into `ValidationTexts` yet, so they read in English.

## Todo

Before the commit:

- [x] Run the two E2E `StationsTests` (`TheOwnerSeesTheirStationsAndRenamesOne`, `TheMenuLeadsToTheStations`). The
  suite boots an AppHost of its own, so the dev AppHost stops first.
- [ ] Run the mobile E2E suite on the emulator: `Public.Api` reads the forecasts through `ReadOnlyWeatherDbContext` now.
- [ ] Commit the emitter repo: the library output (`output-type: library`, `library-namespace`, scenario
  `library-namespace`) is uncommitted on `main`, and `main` has diverged from `origin/main`.
- [ ] Commit the template in one commit. Some renames and deletions are already staged by `git mv`/`git rm`.
  `FocusTemplate.slnx` also carries the MAUI `<Deploy>` hunk from before the spike: in or out.

Emitter:

- [x] Generate the aggregate mapping methods. The `api` output also writes one `Map{Feature}Endpoints()` per namespace
  below the service (`MapUserManagementEndpoints()` calling `MapGroupsEndpoints()` and `MapUsersEndpoints()`) and one
  `Map{Service}Endpoints()` per service (`MapWeatherEndpoints()`, `MapAdminEndpoints()`). The per-interface methods
  stay. Snapshot in the `feature-namespaces` scenario, bump per `docs/spec-first.md`. Afterwards weather's hand-written
  `WeatherEndpoints` goes, and `Admin.Api/Program.cs` maps `MapDemoProfilesEndpoints()`,
  `MapUserManagementEndpoints()`, `MapWeatherEndpoints()`.
- [x] Generated registration: `Add{Service}Endpoints()` beside `Map{Service}Endpoints()` puts the contracts' JSON
  context first in the resolver chain and registers the validators. `AddWeather()` and the Admin `Program.cs` call it.
  One emitter change with the item above and the validation texts under Foundation (`ServiceEndpointsWriter`, `ContractTexts`).

Composition:

- [x] `PageAssembly`: a record `PageAssembly(Assembly Assembly)` in `Admin.Web.Foundation`, registered by each
  feature's `Add…Pages()`. `App.razor` injects `IEnumerable<PageAssembly>` for `AdditionalAssemblies`, and the
  hand-kept `FeatureAssemblies` array goes.
- [x] Constants for the resource and connection names (`FocusTemplate.Names`). `"admin-api"`: `Admin.Web/Program.cs` (`AddWeatherPages`), the
  DemoProfiles and UserManagement `ServiceCollectionExtensions`, the AppHost (`AddProject`, `AuthenticationExtensions`),
  `KeycloakRealmTests`. `"focusdb"` and `"focusdb-readonly"`: `Admin.Api/Program.cs` (including `AddWeather`),
  `Public.Api/Program.cs`, the AppHost, `ApiFixture`. Open: where they live, since the AppHost, the APIs, the WASM
  client and the tests share no project today.
- [x] ~~`AddFeatures()`/`MapFeatures()` builder~~ dropped (2026-10-13): with the generated registration and mapping a
  feature takes three lines in a host (`AddWeather()`, `MapWeatherEndpoints()`, `AddWeatherPages()`), each visible
  where it belongs, which leaves a builder nothing to save.
- [ ] Menu entries stay in the host's `MainLayout`. If a feature should contribute its own, `PageAssembly` grows into
  an `IFeaturePages` interface.
- [x] One OpenAPI document per service. Today `admin-api` serves only its own (`/openapi/v1.yaml`), and weather's
  `openapi.yaml` is a `None` item that never reaches the host's output. Each Api project copies its document as
  `<Content Include="openapi.yaml" Link="openapi/<name>.yaml" CopyToOutputDirectory="PreserveNewest" />` (`admin`,
  `weather`, `public`), so two files named `openapi.yaml` no longer collide in the host's `bin`. The host serves
  `/openapi/{documentName}.yaml` from that folder, limited to known names, 404 otherwise. `/openapi/v1.yaml` becomes
  `/openapi/admin.yaml`: update `docs/spec-first.md`. With Scalar (backlog 1.1) one page per API lists the documents
  with `AddDocument("admin", …, isDefault: true).AddDocument("weather", …)`.

Foundation:

- [x] A feature's display names and pattern messages in validation texts. FluentValidation's sentences translate
  (the global `LanguageManager`), but the weather rules ask `WeatherTexts`, which nothing sets, and `Weather.Shared`
  has no contract resx, so a field name reads in English inside a German sentence (invisible today: `Name` is the same
  word). Suggestion, in the emitter bump with the aggregate mappings: with `library-namespace`, the rules ask one texts
  hook in the library, which `ValidationTexts.UseResources()` sets once, and a feature only adds its resx.
- [x] `HasPermissionAsync(this IAuthorizationService, ClaimsPrincipal, Permission)` in `Api.Foundation` (namespace
  `FocusTemplate.Api.Foundation.Authorization`, which needs a reference to `Http` for `PermissionRequirement`), so
  `StationsRenameHandler.MayManageStationsAsync` shrinks to one call. A scoped service wrapping the accessor as well
  comes when a second handler checks a permission this way.
- [ ] `Client.Foundation` split: the UI-free pieces of `Admin.Web.Foundation` (`AsyncCommand`, `BusyState`,
  `Debouncer`, `LiveUpdates<T>`, `ApiFailureMessages` with its resx, `IViewModel`) move into a `net11.0` project on
  `Http`, ready for a `Public.Mobile.Foundation`. About half a day, a mechanical move.
- [ ] `Public.Mobile.Foundation` (page lifecycle, state display, no forms), with the second real mobile screen.
- [ ] Decide on `src/foundation/` for `Primitives`, `Http`, `Data.Foundation` and `Api.Foundation`. Touches the
  solution file, the project references, `gen.mjs` and the rule globs; worth it if the flat `src/` confuses.

Decisions (closed 2026-10-13):

- [x] No `BffClaimTypes.Permission` alias: `Permission.ClaimType` stays the one name, next to the type it describes.
- [x] "Foundation" stays. It carries a checkable rule (domain- and host-neutral, built on by features), and "Common"
  next to "Shared" would give two vague words. `Infrastructure/` stays in the host, since it is how this host signs
  users in.

Lamama:

- [x] Update `lamama.md`: steps 1, 3 and 5 change with feature packages (see **Afterwards**).

Found along the way:

- [ ] Persistent BFF session cookie. `/bff/login` challenges without `IsPersistent`, so `__Host-focus.session` has no
  `Expires`, and Firefox (default startup) drops it on close: every browser start signs out of the app, and without
  Keycloak's "Remember me" the password is asked again. Data Protection keys (`%LOCALAPPDATA%`) and the persistent
  Keycloak already carry a session across AppHost restarts. Fix: `IsPersistent = true` at login, cookie
  `ExpireTimeSpan` 7 days with sliding expiration (the realm's `ssoSessionIdleTimeout`), Keycloak's 30-day maximum
  still ends it through the failing refresh. Trade-off: the app stays signed in across browser restarts without
  "Remember me". Test-first in the E2E suite (the login's cookie carries an expiry), then the BFF, then a line in
  `docs/authentication.md`. Until then: Firefox "Open previous windows and tabs" keeps session cookies.

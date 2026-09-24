# Weather stations — plan and where we stand

*Written 2026-09-22 at the end of the first implementation session. Companion to backlog.md §10 (the decision record)
and typed-ids.md (the id findings). This file is the hand-over: what is decided, what is on disk, what is verified,
what comes next in which order.*

## Goal

`WeatherForecast` as one four-column table cannot carry the DDD wave (§2). The sample domain becomes **weather
stations**: Admin administers stations scoped to their owner, Public reads active stations with current values,
forecasts and open alerts. Measurements come from a simulated ingestion, its own AppHost resource `observer`. The old
table stays as the forecast aggregate, so today's code, migration and E2E tests remain valid.

## Decisions (all made, see backlog §10 for the reasoning)

| Topic | Decision |
|---|---|
| Aggregates | `Station` (uuid v7, self-assigned), `Observation` (int64 identity, append-only), `WeatherForecast` (int32 identity, kept), `Alert` (uuid v7). References by id only, FK constraints with cascade. |
| Ids | Vogen structs from `src/spec/primitives.tsp`: `UserId`, `StationId`, `ObservationId`, `WeatherForecastId`, `AlertId`. The type says what it identifies, the property the role (`Station.OwnerId : UserId`). |
| Well-known users | `WellKnownUsers.System` = `00000000-0000-7000-8000-000000000000` (no person acting), `WellKnownUsers.Developer` = `…0001` (owner of the seed, acting user until Keycloak; the realm import must give the dev account this id), `ApiFixture.TestUser` = `…c0de`. All at v7 timestamp zero, none is `Guid.Empty` (the Unspecified sentinel). |
| Lifecycle | **Stateless 5.20.1** inside `Station`: `Active → Maintenance` (guard: no open alerts), `Maintenance → Active`, `* → Retired` (final). `DescribeLifecycle()` exports the DOT graph. Long-running workflows stay with backlog 2.6. |
| Value objects | `Coordinates` over a PostGIS `geography (point)` (NetTopologySuite 10.0.3), `AlertThresholds` as EF complex type. |
| Enums | `StationStatus`, `AlertKind` as native Postgres enums via `MapEnum` in `UseAppDbContextProvider()`. |
| Auditing | Marker `IAuditable` + EF shadow columns (`CreatedAt/By`, `UpdatedAt/By` as `UserId`), stamped by `AuditingInterceptor` (`TimeProvider` + `ICurrentUser`), attached through `AddAppDbContextAuditing()`. No DB trigger (cannot know the user). Convention: no `ExecuteUpdate` on `IAuditable` types. Replaces the `Auditable` base class of backlog 2.2. |
| Design style | Khorikov: always-valid aggregates, no public setters/ctors, `Create(...)` + intention-revealing methods, results as C# 15 unions (`StationTransition`, `StationEdit`), no `Result` library. Validation in four layers: shape at the boundary (400), access in the handler (404, also for foreign stations), invariants in the aggregate (409/422), database last (unique index, concurrency → 409). |
| Persistence | Entities are the persistence model, one `IEntityTypeConfiguration<T>` per aggregate, `DbContext` is the unit of work, reads project straight into DTOs. Postgres image `postgis/postgis:17-3.5` in AppHost and Testcontainers. |
| Still to pick up | Mapperly for the read model (decided, not yet added), Bogus object mothers, NetArchTest, `xmin` concurrency token, FluentValidation only with the admin write UI. |

## On disk (uncommitted in ai-template)

Everything below builds (`dotnet build FocusTemplate.slnx` green) and the Admin integration tests pass 6/6 against a
PostGIS Testcontainer. The emitter repository has nothing pending for this work.

- `src/spec/primitives.tsp` — five ids with `@friendlyName`; `src/FocusTemplate.Primitives/{UserId,StationId,AlertId}.cs` — hand-written `New()` partials.
- `src/FocusTemplate.Data/Domain/` — `Entity<TId>`, `AggregateRoot<TId>` (collects `IDomainEvent`s, `PopDomainEvents`), `IDomainEvent`, `IAuditable`, `ICurrentUser`, `FixedCurrentUser`, `WellKnownUsers`.
- `src/FocusTemplate.Data/Stations/` — `Station`, `StationStatus`, `Coordinates`, `AlertThresholds`, `StationTransition`/`StationEdit` unions with `Transitioned`/`Edited`/`NotAllowed`, events `StationStatusChanged`/`ThresholdExceeded`, `StationConfiguration`.
- `src/FocusTemplate.Data/Observations/`, `Alerts/`, `WeatherForecasts/` — aggregate + configuration each. `Entities/WeatherForecast.cs` is gone (namespace is now `FocusTemplate.Data.WeatherForecasts`).
- `src/FocusTemplate.Data/Auditing/` — interceptor, `AddAuditingShadowProperties()`, `AddAppDbContextAuditing()`.
- `src/FocusTemplate.Data/AppDbContext.cs` (DbSets, `HasPostgresExtension("postgis")`, configurations from assembly, shadow properties), `AppDbContextOptionsExtensions.cs` (`UseAppDbContextProvider`), `VogenEfCoreConverters.cs` (five markers), `AppDbContextFactory.cs`, `WeatherSeed.cs` (two stations Vienna/Graz, five forecasts, three observations, `ReloadTypes()` before inserting).
- `src/FocusTemplate.Data/Migrations/20260922133657_WeatherStations.*` — extension, enums, tables, audit columns, indexes.
- AppHost: `.WithImage("postgis/postgis").WithImageTag("17-3.5")`. Admin `Program.cs`: provider options, `ICurrentUser` = `Developer`, auditing. Public `Program.cs`: provider options (the model contains the `Point`, so Public needs the plugin although it only reads forecasts).
- Tests: `PostgresFixture` on the postgis image, `ApiFixture.CreateDbContext()` with provider options + interceptor, `TestUser` registered in the API under test, `WeatherForecastTests` arrange a station first, new test for distinct store-generated ids.
- Docs: backlog §10 + changes to 2.2/2.3/§6, AGENTS.md (Data layout, design rules, PostGIS, auditing convention), `.editorconfig` carve-out for the two union files.
- Packages: `Stateless` 5.20.1, `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` 10.0.3 in `Directory.Packages.props`; many `packages.lock.json` changed accordingly.

## Verified

- `dotnet test tests/FocusTemplate.Admin.Api.IntegrationTests`: 6/6 (fresh build).
- Migrations + seed against a fresh `postgis/postgis:17-3.5` container through the design-time factory, i.e. the path the
  `migrations` resource takes: two stations (`active`, thresholds, `POINT(16.4108 48.2263)`, `CreatedBy` = Developer,
  `CreatedAt = UpdatedAt`), 5 forecasts, 3 observations, enum `station_status` = `{active,maintenance,retired}`.
- Not yet run: `aspire start` end to end, the Playwright E2E, the mobile E2E.

## Gotchas found (keep)

- **Npgsql type catalog after `CREATE EXTENSION`**: the seed runs on the connection that just created `postgis`; without
  `NpgsqlConnection.ReloadTypes()` the first geography insert fails ("type 'geography' could not be found"). Handled in
  `WeatherSeed.ReloadTypes`. The APIs are unaffected, they connect after the migrations resource.
- **StyleCop 1.2.0-beta.556 crashes on C# 15 `union` declarations** (AD0001 in SA1649). The two union files are marked
  `generated_code = true` in `.editorconfig`; the records they name stay analyzed. Revisit when StyleCop learns unions.
- **IDE0055 at end-of-line positions means line endings**, not layout: a file went LF. Check with
  `tr -cd '\r' < file | wc -c`, fix with `perl -pi -e 's/\r?\n/\r\n/' file`.
- Object initializers with four members must be one member per line, else IDE0055.
- `dotnet test --no-build` after a failed build runs the previous binaries and reports green: never trust a green run
  without a green build in the same command.

## Next steps, in order

1. **Commit** the working tree (ai-template only; suggested subject: `Model the weather station domain`).
2. **Test extraction**: move `PostgresFixture`, `ApiFixture`, `ApiTestBase` into `tests/FocusTemplate.IntegrationTesting`,
   add **Bogus** object mothers (`StationMother`, `ObservationMother`, `ForecastMother`), create
   `tests/FocusTemplate.Public.Api.IntegrationTests` on the same base (backlog 8.2). Reset must clear Stations too
   (it does, Respawn is FK-aware; `ResetClearsAllRows` asserts it).
3. **Admin write path for stations** (spec-first): `src/admin/spec/Stations.tsp` with create/rename/describe/relocate/
   thresholds/start-maintenance/reactivate/retire/delete, owner scoping in every handler (foreign → 404), the unions
   mapped to 409, `Problem<404 | 409>`; TypeSpec `@minValue/@maxValue` on coordinates and thresholds; `@authorize`
   later. Handlers load the aggregate, call the method, `SaveChanges`.
4. **`observer` resource**: worker project `src/FocusTemplate.Observer`, `ICurrentUser` = `WellKnownUsers.System`,
   `AddAppDbContextAuditing()`, `Features:Observer` flag, periodic `Observation.Record` for active stations, then
   `station.Evaluate(...)` to raise `ThresholdExceeded`.
5. **Event dispatch (backlog 2.6)**: `SaveChangesInterceptor` on `SavedChanges` popping `AggregateRoot.PopDomainEvents()`
   and publishing through Mediator `INotification` wrappers; handler for `ThresholdExceeded` → `Alert.Raise`.
6. **Public read model** with **Mapperly**: `src/public/spec/Stations.tsp`, active stations paged, optional radius
   (`EF.Functions.Distance` on the geography column), latest observation, forecasts, open alerts; `xmin` concurrency
   token on `Station` when the first concurrent write path exists.
7. **Architecture tests (2.7)** with NetArchTest: the two `Shared` projects never reference each other, `Primitives`
   references no EF Core, `Data` types outside `Domain/` derive from the bases, `IAuditable` only on aggregate roots,
   `Station.DescribeLifecycle()` matches the committed graph in the docs.

## Open questions for later

- Mapperly for the read model or hand projections (decided Mapperly, revisit when the first projection exists).
- Whether Public gets a write path (alert subscriptions) — would pull idempotency and auth forward.
- Keycloak realm import must carry `WellKnownUsers.Developer` as the dev account id (3.2).

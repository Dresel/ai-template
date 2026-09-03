# FocusTemplate — Backlog

*Compiled 2026-07-14 from the feature wishlist + research over SSW.VerticalSliceArchitecture,
jasontaylordev/ardalis/amantinband/nadirbad templates, dotnet/eShop, fullstackhero/dotnet-starter-kit,
practical-dotnet-aspire, booking-microservices, Wolverine samples, and a library fact-check (versions
as of 2026-07).*

**Decisions already made:**

- DDD shape: **feature folders in `FocusTemplate.Api`** (endpoint + validator + mapper per use case), **domain stays in `FocusTemplate.Data`** (aggregates, value objects, EF config). No new projects.
- Strongly-typed IDs: **Vogen**; richer value objects: **EF Core 10 complex types**.
- Eventing library: **open** — shortlist researched, see [2.6](#26-domain-events--a-simple-event-handler--m--decision-pending).
- Two verticals *(2026-07-14)*: **Admin** (today's API/BFF/WASM stack, stays internal-only) + **Public** (new deliberately-exposed API + **native MAUI** mobile client on the .NET 11 preview workloads; Blazor Hybrid rejected). Naming is audience/exposure-based. See [§8](#8-public-vertical--native-maui-mobile-decided-2026-07-14).

**Open questions** (marked ❓ inline): ErrorOr adoption (1.2), eventing pick (2.6), feature flags for cache/auth (3.1/3.2), meaning of "remove validation" (4.3), which §6 candidates to promote, DeviceRunners spike (8.4).

Sections are ordered as suggested waves: 0 → 1 → 2 → 3 → 4; the §8 mobile vertical starts with a rename (8.1) that must land **before** wave 2, while 8.2+ can run parallel to waves 3–4. Resolve ❓2.6 before implementing 2.1 (the choice changes endpoint wiring).

---

## 0. Housekeeping

### 0.1 Commit the working tree — S
The entire EF Core + testing + permissions + docs work is uncommitted (multi-session working tree; `git clean -xfd` would destroy it). Commit before starting anything below; this backlog too.

---

## 1. Foundations

### 1.1 OpenAPI + Scalar — S
**Goal:** browsable, accurate API reference; the base other items (auth docs, pagination docs) build on.

- `AddOpenApi()` already exists → add **Scalar.AspNetCore** (2.16.11, MIT, zero deps): `MapScalarApiReference()` dev-only on the API.
- XML doc comments flow into the OpenAPI doc via the .NET 10 source generator: set `<GenerateDocumentationFile>true</GenerateDocumentationFile>` (Api + Shared). Gotcha: only literal document-name `AddOpenApi(...)` overloads are intercepted (aspnetcore #65417).
- operationIds: keep `WithName(...)` per endpoint; adopt Jason Taylor's convention (handler method name → operationId) when feature folders land — enables typed client generation later.
- API is internal-only: in dev its localhost endpoint is still reachable — add `WithUrls` display text ("Scalar UI") on the API resource so the dashboard links straight to it.
- Later (with 3.2): eShop-style **OpenAPI document transformers** — auto-add 401/403 + security scheme to `[Authorize]` endpoints.

### 1.2 Error handling & ProblemDetails — M
**Goal:** every failure, exception or expected, leaves the API as RFC 9457 ProblemDetails; one mapper, no per-endpoint ceremony. Prerequisite for the UI flow (§4).

- `IExceptionHandler` implementation mapping unhandled/known exceptions → ProblemDetails with `traceId`, `requestId`, `instance` (`"GET /path"`) enrichment (SSW pattern — pairs with the OTel story).
- ❓ **Errors-as-values:** recommend **ErrorOr** (2.0.1, MIT) + per-aggregate error catalogs (`WeatherErrors.NotFound`) + a single ErrorType→HTTP helper for minimal APIs (nadirbad's `MinimalApiProblemHelper` shape). 3 of 4 surveyed templates converged on errors-as-values; Jason Taylor (exceptions) is the outlier. Alternative: plain `TypedResults` + exceptions, zero deps.
- Validation failures → `HttpValidationProblemDetails` (errors dictionary) — the contract 4.1/4.2 consume.

### 1.3 Mapperly — S
**Goal:** replace hand-written entity→DTO projection with source-generated mapping.

- **Riok.Mapperly 4.3.1** (Apache-2.0 — the free winner; AutoMapper went commercial).
- `[Mapper]` static partial class per feature folder; map **entity → DTO direction only**.
- Caveats to encode as conventions: private setters/required ctor params on DDD entities don't map — fine, DTOs are the target; Vogen IDs/value objects need small user-implemented mapping methods in the mapper class (Mapperly picks them up automatically); never map DTO → tracked entity (mutate via aggregate methods instead).

### 1.4 Custom OTel metrics (API) — S/M
**Goal:** demonstrate first-class custom metrics next to the built-in ones; pattern other features (cache, eventing) reuse.

- Per-concern telemetry class: **`IMeterFactory`** (+ `TimeProvider` for durations), DI singleton — never static meters (testability). practical-dotnet-aspire's `CommandHandlerMetrics` is the reference shape.
- Semconv naming: lowercase dot-namespaced (`focustemplate.weather.lookups`), UCUM units in the `unit` param (`"s"`, `"{lookup}"`), durations = histograms **in seconds**.
- Register additively at the feature's DI extension: `services.AddOpenTelemetry().WithMetrics(m => m.AddMeter(WeatherTelemetry.MeterName))` (eShop's trick — ServiceDefaults stays generic).
- **Exemplars**: record measurements inside an active `Activity` → Aspire dashboard links metric points to traces (on by default since OTel SDK 1.9).
- Unit-test with `MetricCollector<T>` (Microsoft.Extensions.Diagnostics.Testing).

---

## 2. Domain (DDD wave)

### 2.1 Feature folders in the API — M
**Goal:** SSW-VSA shape mapped onto existing projects.

- `Features/Weather/GetWeatherForecast/` holding `GetWeatherForecastEndpoint.cs`, `...Validator.cs`, `...Mapper.cs`; **response DTOs stay in `FocusTemplate.Shared`** (deliberate deviation from SSW co-location — the WASM client needs them).
- **Explicit registration** — static `MapWeatherEndpoints()` extension per feature group, called from `Program.cs`. No reflection discovery (SSW's own `FeatureDiscovery` is marked "TODO: source generate"); explicit = compile errors = agent-legible.
- **Partial DbContext per feature**: `AppDbContext.Weather.cs` (keeps the context from becoming a god file).
- Interacts with ❓2.6: if Immediate.Apis is chosen, endpoint wiring is generated from handler attributes instead.

### 2.2 DDD base types + auditing — M
**Goal:** the reusable domain kernel.

- `Entity<TId>` → `AggregateRoot<TId> : IAggregateRoot` with `AddDomainEvent`/`PopDomainEvents`; `Auditable` base (`CreatedAt/CreatedBy/UpdatedAt/UpdatedBy`).
- **Audit `SaveChangesInterceptor`** fed by `TimeProvider` + an `ICurrentUser` abstraction (stub until 3.2 lands); include the `HasChangedOwnedEntities()` trick so owned/complex-type changes stamp the parent (jt + SSW both do this).
- `AuditableConfiguration<T>` base `IEntityTypeConfiguration`; entities get **private parameterless ctors** (EF) + static `Create(...)` factories using `Guid.CreateVersion7()`.
- Invariants via guard-style property setters; domain error catalogs pair with 1.2.

### 2.3 Vogen strongly-typed IDs — S *(decided)*
- **Vogen 8.0.6** (Apache-2.0, active): `[ValueObject<Guid>] public readonly partial struct WeatherForecastId;` co-located with the aggregate.
- EF wiring (the SSW/ardalis gem): one `internal sealed partial class VogenEfCoreConverters` stacked with `[EfCoreConverter<WeatherForecastId>]` attributes + `configurationBuilder.RegisterAllInVogenEfCoreConverters()` in `ConfigureConventions` — one attribute line per new ID. Add a v7-Guid `ValueGenerator` (ardalis has one).
- Wire-contract boundary: DTOs in Shared keep primitives (`Guid`); convert in the Mapperly mapper.

### 2.4 Value objects as EF10 complex types — M *(decided)*
- Rich VOs as `record`/`record struct` mapped via **complex types** (`ComplexProperty`, optionally `ToJson()`) — the EF-sanctioned mechanism; avoids the long-standing "LINQ can't query into value-converted members" trap that whole-object converters hit.
- Child collections: `OwnsMany(...).ToJson()` (SSW's `Power` example).
- Demo candidate on the weather aggregate: e.g. `Temperature` (value + unit invariants) — also a natural Mapperly user-implemented-mapping example.

### 2.5 Postgres enums (`MapEnum`) — S
- Introduce a real enum (e.g. `WeatherCondition`) as a **native Postgres enum**: `UseNpgsql(o => o.MapEnum<WeatherCondition>("weather_condition"))` — one call configures model + wire mapping + migrations.
- **Aspire gotcha (verified):** `AddNpgsqlDbContext` passes an `NpgsqlDataSource`, so ALSO call `MapEnum<T>()` on the `NpgsqlDataSourceBuilder` in `ConfigureDataSourceBuilder` — both places or runtime failures.
- Migrations emit `CREATE TYPE` automatically; adding CLR values → auto `ALTER TYPE ... ADD VALUE`; **dropping/renaming values is manual SQL** — document. Lowercase snake_case type name; update seed + Shared contract (enum in Shared is fine — it's part of the wire contract).

### 2.6 Domain events + a simple event handler — M ❓ *(decision pending)*
**Pattern (independent of library):** events collected on the aggregate → dispatched by a `SaveChangesInterceptor` on **SavedChanges** (post-commit) → handlers. At-most-once semantics documented; outbox (§6) is the durability upgrade path. Handler failures logged, never rethrown into the save path (FSH rung 1).

**Library shortlist (researched 2026-07-14):**

| | License / health | Events (multi-handler) | Endpoints story | OTel / AOT | Fit notes |
|---|---|---|---|---|---|
| **martinothamar/Mediator** 3.0.2 | MIT; 6.8M dl; quiet since 03/2026, bus factor 1, prior 2.5y release gap | ✔ `INotification`, pluggable publishers (foreach/whenall) | manual `mediator.Send` from endpoints | ✔ built-in semconv OTel; ✔ NativeAOT | Safest: MediatR-shaped, source-gen, compile errors. Risks tolerable (generated code lives in your build). |
| **Immediate.Handlers 3.10 + Immediate.Apis 6** | MIT; active; **ships net11.0 TFM**; bus factor 1 | ✘ **by design** — one handler per request; fan-out = DIY | ★ best-in-field: `[MapGet]` on handler generates the minimal-API endpoint | ✘ DIY behavior; likely AOT-safe | Handler *is* the slice — replaces 2.1's hand wiring. Needs ~40 lines of plain-DI event fan-out anyway. |
| **Foundatio.Mediator** 1.3.3 | Apache-2.0; created 2025-07, ~12K dl — early-adopter bet | ✔ strategies (foreach/whenall/fire-and-forget) + ordering | ✔ auto-generates endpoints from handlers | ✔ OTel generated; AOT undocumented | Feature-richest single answer (middleware, cascading messages, `Result<T>`, SSE). Convention-based discovery = magic; pin `HandlerDiscovery.Explicit`. |
| **Plain DI** (`IEnumerable<IDomainEventHandler<T>>` + optional `Channel<T>`) | zero deps | ✔ hand-rolled (~40 lines) | n/a | manual `ActivitySource` | Most agent-transparent; you own ordering/strategies as needs grow. |

Also-rans: Wolverine (MIT, hyper-active — right answer if durable outbox is wanted *now*, but a whole framework); Brighter/SlimMessageBus/LiteBus/Cortex (reflection-based, smaller); MassTransit v9 commercial (v8 security-fixes only through 2026) — dead end.

**Recommendation:** default = **martinothamar/Mediator** (only proven source-gen option with real `INotification` fan-out + OTel). Most-AI-transparent alternative = **Immediate.Apis for endpoints + plain-DI event dispatcher**. Foundatio is worth a time-boxed spike if its endpoint+events+OTel combo appeals.

### 2.7 Architecture tests — S
- New `tests/FocusTemplate.ArchitectureTests` with **NetArchTest.Rules** (1.3.2): domain types derive from the 2.2 bases; entities have private parameterless ctors; `FocusTemplate.Shared` never references Data/EF; feature folders don't reference each other; DTOs only in Shared.
- Outsized value here: machine-checkable conventions are guardrails *for AI agents* (SSW + FSH both do this).

---

## 3. Platform features

### 3.1 FusionCache — M
**Goal:** show L1 (memory) + L2 (distributed) + cross-node invalidation, observable in the dashboard.

- **ZiggyCreatures.FusionCache 2.6.0** (MIT): L1 memory; L2 Redis via **Aspire `AddRedis`** + `Aspire.StackExchange.Redis` client + `WithDistributedCache` (+ SystemTextJson serializer package).
- **Backplane** `...Backplane.StackExchangeRedis` — cross-node L1 invalidation (the reason FusionCache over built-in `HybridCache`: MS's has **no backplane** — other nodes' L1 stays stale until TTL; dotnet/runtime#125602. Also missing: fail-safe, soft timeouts, eager refresh).
- **Tagging** demo: cached weather read tagged `"weather"`; a write endpoint calls `RemoveByTag("weather")` — visible invalidation. Optionally expose via `.AsHybridCache()` so app code targets the MS abstraction.
- **OTel**: `ZiggyCreatures.FusionCache.OpenTelemetry` → traces + metrics in the dashboard (rides the 1.4 pattern).
- **Scale-out proof** (per AGENTS.md testing rules): `WithReplicas(2)` on the API + a test that instance B observes instance A's invalidation via the backplane.
- ❓ Feature flag `Features:Cache`? Redis adds a container to every `aspire start`.

### 3.2 Keycloak authN/authZ (BFF pattern) — XL
**Goal:** the Duende-BFF feature set (cookie auth, server-side sessions, backchannel logout) without Duende — **Duende BFF v4 is paid-in-production**; hand-rolled OIDC+cookie+YARP is the mainstream free path.

- **AppHost:** `Aspire.Hosting.Keycloak` (**preview** 13.x; GA on the Aspire roadmap): `AddKeycloak("keycloak", 8080)` (stable port — cookie survival across restarts) + `WithRealmImport("./Realms")` (dev-only; deterministic, matches the no-data-volume philosophy) + `WithOtlpExporter()`. Publish: realm baked via `WithDockerfile` (documented production alternative).
- **BFF:**
  - Cookie + OIDC code flow; **PAR is on by default** (.NET 9+) and Keycloak supports it.
  - **Server-side sessions:** `ITicketStore` (`CookieAuthenticationOptions.SessionStore`) backed by Redis/FusionCache, keyed by `sid` → revocable sessions (this is what makes backchannel logout meaningful). Depends on 3.1's Redis (or memory fallback).
  - **Backchannel logout endpoint** (still no built-in in .NET 10 — aspnetcore #4659): validate `logout_token` (`typ=logout+jwt`, `events` claim, `sid`/`sub`) → revoke the matching ticket. Reference impl: damienbod/keycloak-backchannel.
  - Login/logout endpoints + `/user` claims endpoint for the WASM client; CSRF defense on `/_api` (SameSite=Strict + required custom header); YARP transform attaches the access token to proxied API calls + refresh handling (evaluate `Duende.AccessTokenManagement` — ❗verify its current license — vs hand-rolling; `Keycloak.AuthServices` 3.0 (MIT) as helper candidate).
- **API:** JwtBearer via `Aspire.Keycloak.Authentication` (preview) or manual authority; per-API audience; one policy/scope-gated sample endpoint (then wire 1.1's auto-401/403 OpenAPI transformer).
- **WASM:** `AuthenticationStateProvider` reading the BFF `/user` endpoint; route guards; login/logout UI. `ICurrentUser` from 2.2 becomes real.
- **Tests:** realm JSON with test users; Playwright login-flow E2E; API integration tests via a test auth scheme.
- ❓ Behind `Features:Authentication` so the template runs without Keycloak?

---

## 4. Opinionated UI flow

### 4.1 Shared validation, defined once — M
- **FluentValidation 12.1.1** (Apache-2.0 — the commercialization rumor is false). Validators for request DTOs live in **`FocusTemplate.Shared`** → one definition, two enforcement points:
  - Client: **Blazorise.FluentValidation 2.2.1** (verified; same version line as the existing Blazorise packages) — `<Validations HandlerType="typeof(FluentValidationHandler)">`.
  - Server: small endpoint filter running the same validator → `HttpValidationProblemDetails` (or a pipeline behavior if a mediator wins ❓2.6).
- .NET 10's built-in `AddValidation()` (DataAnnotations-based) rejected: no async/DI-dependent rules, and it can't share with Blazorise.

### 4.2 Centralized client-side error handling — M
- Typed `ApiClient` wrapper over `HttpClient`: parses ProblemDetails / ValidationProblemDetails (1.2's contract) into a result type; **field errors** feed Blazorise validation display, **non-field errors** → toast/notification service; top-level `ErrorBoundary`; standard busy/loading pattern.
- Folds in the carried item "harden the Weather page fetch" — the wrapper *is* the try/catch, once, for every page.

### 4.3 Form + grid patterns — M
- Opinionated form wrapper (submit-busy, disable-on-submit, server-error mapping) so a new form = DTO + validator + fields, nothing else.
- **DataGrid** inline-edit validation reusing the same shared validators.
- Delete/remove flow with a confirmation-dialog pattern. ❓ "remove validation" from the wishlist interpreted as *validation/confirmation on remove operations* — correct me if it meant something else.

### 4.4 Pagination convention — S
- `PagedResult<T>` in Shared + a `ToPagedResultAsync` EF helper; DataGrid server-side paging via `ReadData`; paging params documented in OpenAPI.

---

## 5. Carried over (from summary.md)

- **EF Core 11 bump** — blocked on stable `Npgsql.EntityFrameworkCore.PostgreSQL` 11 (11.0.0-preview.5 exists); move runtime/Design/Relational/provider/`dotnet-ef` as one unit.
- **Prod OTLP config** — documented in summary.md, deliberately unimplemented.
- **.NET 11 preview6:** real logger in `BackgroundExportHandler` (aspire #18272).
- **Forwarded-headers integration test** (scheme/host/client-IP restored + untrusted source ignored).
- **`/_otlp` rate limiter + body-size cap** — superseded by the broader rate-limiting candidate in §6; keep at least the minimal fix if §6 is deferred.
- ~~Weather page try/catch~~ → folded into 4.2.

---

## 6. Candidates from research — ❓ promote or drop

| Idea | Source | Why | Size |
|---|---|---|---|
| **Idempotency-Key endpoint filter** (opt-in header, cached replay, `Idempotency-Replayed` header) | FSH | Cheap, high-value API hardening; pairs with FusionCache | S |
| **Rate limiting + security-headers middleware** on BFF/ingress | FSH | Closes the `/_otlp` open-relay gap properly | S/M |
| **EntityFrameworkCore.Exceptions.PostgreSQL** | SSW | Typed `UniqueConstraintException` etc. → clean 409 mapping | S |
| **Outbox** (EF table + dispatcher, dead-letter + redrive + metrics) | FSH/eShop | The durability rung above 2.6 when needed | M/L |
| **WireMock.NET as an Aspire resource** | practical-aspire | Stub third-party HTTP in integration tests | S |
| **AppHost polish**: `ContainerLifetime.Persistent` on infra, `WithUrls` display text, pgAdmin sidecar, Npgsql `Minimum Pool Size` floor | eShop/FSH | Dev-loop speed + dashboard ergonomics | S |
| **OTel tuning**: dev `AlwaysOnSampler`, filter health-check spans, 10s metric export interval | eShop/FSH | Dashboard freshness, less noise | S |
| **ADRs in-repo** (`docs/adr`) + `.claude/rules/*.md` per topic | SSW | Decision memory for agents; SSW pairs ADRs with rules | S |
| **Microsoft.FeatureManagement** (+ `FeatureGateEndpointFilter`) | FSH | Runtime flags vs. today's AppHost-config-only flags | M |
| **Bogus test-data factories** (object mothers in a shared test project) | SSW/amantinband | Arrange ergonomics as entities grow | S |
| **Optimistic concurrency demo** (Postgres `xmin`) + 409 ProblemDetails | — | Postgres-native, DDD-adjacent, cheap to show | S/M |
| **CI (GitHub Actions)**: build + test (Docker for Testcontainers, Playwright container) | — | Nothing runs in CI today | M |
| **Subcutaneous test tier** (dispatch through the pipeline without HTTP) | amantinband/jt | Only worth it if a mediator wins 2.6 | note |

---

## 7. Explicitly not doing (and why)

- **MediatR / AutoMapper / MassTransit v9 / FluentAssertions v8** — commercial relicensing; free picks made instead (Mapperly, xunit asserts; mediator per 2.6). The 2025-26 template ecosystem made the same exits.
- **Duende BFF / IdentityServer** — paid in production; Keycloak + hand-rolled BFF (3.2).
- **StronglyTypedId** — dormant since 2024 → Vogen.
- **Repository/UoW abstraction** — `DbContext` *is* the UoW (jt ADR-001 rationale: repositories hide, not remove, EF coupling). `Ardalis.Specification` only if query reuse ever demands it.
- **Built-in `HybridCache` alone** — no backplane → stale L1 on other nodes (see 3.1).
- **Swashbuckle** — replaced by built-in OpenAPI + Scalar.
- **API versioning (Asp.Versioning)** — deferred until the template has consumers that need it; integrates with `AddOpenApi` when the day comes.
- **EF InMemory provider for tests** — real Postgres via Testcontainers stays the rule.
- **Blazor Hybrid for the mobile client** — maximal UI reuse (eShop's `HybridApp` + shared `WebAppComponents` is the reference pattern, and Hybrid touches *less* of MAUI's buggy native surface), but §8 decided on a native mobile UX. Revisit only if a public *web* client ever joins the Public vertical.

---

## 8. Public vertical — native MAUI mobile *(decided 2026-07-14)*

**Decision:** split the template into two audience-named verticals — **Admin** (the existing stack) and **Public** (a second, deliberately exposed API + a **native MAUI** mobile client, XAML not Blazor Hybrid) — both on the .NET 11 preview road (MAUI via preview workloads). Naming is exposure-based and `sed`-renameable to persona pairs once a consumer's domain is known (precedent: [eShopSupport](https://github.com/dotnet/eShopSupport)'s `StaffWebUI`/`CustomerWebUI`). The native-client-with-its-own-API-edge shape follows dotnet/eShop (`ClientApp` + `Mobile.Bff.Shopping`).

**Target layout** (spine keeps its names; verticals get physical folders):

```
src/
  FocusTemplate.AppHost | .ServiceDefaults | .Data      # shared spine; ONE domain, two APIs
  admin/
    FocusTemplate.Admin.Api                        # was FocusTemplate.Api (internal-only)
    FocusTemplate.Admin.Web.Bff                     # was FocusTemplate.Web.Bff
    FocusTemplate.Admin.Web                        # was FocusTemplate.Web (WASM)
    FocusTemplate.Admin.Shared                     # was FocusTemplate.Shared (web wire contract)
    FocusTemplate.Admin.Web.ClientServiceDefaults   # was FocusTemplate.Web.ClientServiceDefaults
  public/
    FocusTemplate.Public.Api                            # new; exposed; JwtBearer when 3.2 lands
    FocusTemplate.Public.Shared                         # new; mobile wire contract (screen-shaped DTOs)
    FocusTemplate.Public.Mobile                         # new; native MAUI, net11.0-android;net11.0-ios
    FocusTemplate.Public.Mobile.ServiceDefaults         # new; `dotnet new maui-aspire-servicedefaults`
tests/
  FocusTemplate.IntegrationTesting                      # extracted PostgresFixture/ApiFixture/ApiTestBase
  FocusTemplate.Admin.Api.IntegrationTests         # was Api.IntegrationTests
  FocusTemplate.Admin.Web.E2E                      # was Web.E2E (Playwright, unchanged)
  FocusTemplate.Public.Api.IntegrationTests             # new; same Testcontainers pattern
  FocusTemplate.Public.Mobile.E2E                       # new; Appium + xUnit, thin
```

Aspire resource names follow: `admin-api`, `admin-bff`, `public-api`, `mobile`; rename `api-migrations` → `migrations` (it now gates both APIs). Keycloak clients (3.2) reuse the same names.

### 8.1 Rename to the two-vertical layout — M *(land after §1, BEFORE §2)*
- Mechanical but wide: folders, namespaces, `FocusTemplate.slnx`, AppHost `Projects.*` refs, E2E fixture, launch settings. `data-testid`s untouched. One commit, nothing new added, full existing suite green before and after.
- Do it before the DDD wave so every §2 feature folder is born in the right place.
- The two `Shared` projects must never reference each other — add to the 2.7 architecture-test list.
- Cross-vertical contract primitives (`PagedResult<T>` 4.4, ProblemDetails helpers 1.2): start duplicated, extract a common project only on the second consumer.

### 8.2 `Public.Api` walking skeleton — M *(skeleton ✔ 2026-07-15; hardening open)*
- ✔ New `Public.Api` + `Public.Shared` + one read endpoint over the weather aggregate; standard internal wiring (`ServiceDefaults`, `AddNpgsqlDbContext`, `WaitForCompletion(migrations)`). Wired unconditionally as `public-api`.
- Open: the `FocusTemplate.IntegrationTesting` extraction + `Public.Api.IntegrationTests` (same one-container/db-per-class/Respawn contract).
- **Promotes from §6 by necessity** (public exposure): rate limiting + security headers; idempotency-key filter (mobile networks retry). Pagination (4.4) lands here first.
- Auth split (extends 3.2): BFF keeps the cookie flow; mobile = Keycloak **public client, code + PKCE** → bearer tokens → `Public.Api` JwtBearer. The JwtBearer leg planned in 3.2 serves this for free.
- Natural first consumer for 2.6 + 3.1: admin-side write → domain event → public-side cache invalidation over the shared Redis backplane.

### 8.3 MAUI app + Aspire wiring — L *(done 2026-07-15, except on-device run)*
- ✔ [`Aspire.Hosting.Maui`](https://aspire.dev/integrations/frameworks/maui/) (**preview**): `AddMauiProject("mobile", <csproj path>)` — **path string, never a `ProjectReference`** (TFM clash breaks the AppHost build; same class of trap as the Data `NoWarn ASPIRE004`).
- ✔ Device configs *(decision: no Windows/MacCatalyst targets — mobile-only template)*: `AddAndroidEmulator()` + `AddiOSSimulator()`, both `WithOtlpDevTunnel()` + `WithReference(publicApi, devTunnel)`. Added unconditionally inside the flag — incompatible platforms (iOS on Windows) just show "unsupported" in the dashboard.
- ✔ `Public.Mobile.ServiceDefaults` from `dotnet new maui-aspire-servicedefaults`; typed `WeatherApiClient` via `https+http://public-api` service discovery; `MainPage` weather list with `AutomationId`s.
- ✔ Behind **`Features:Mobile`, default off** (AppHost appsettings + gitignored local override; E2E fixture pins it off) so plain `aspire start` stays light.
- ✔ Versioning: `Microsoft.Maui.Controls` = `VersionOverride="$(MauiVersion)"` (workload-resolved); `Microsoft.Maui.Core` pinned in CPM to the workload band (11.0.0-preview.5.26304.4). Workloads `maui-android`/`maui-ios` installed; Android SDK platform 37 (preview) required.
- Gotchas found: MAUI templates still emit net10-era content (TFMs, `SupportedOSPlatformVersion` 21 vs required 24, missing usings in `maui-aspire-servicedefaults` `Extensions.cs`); `Directory.Build.props`' single `<TargetFramework>` must be cleared with an empty element for multi-targeting; **`dotnet format` corrupts multi-TFM projects** (writes conflict markers) — hand-format or `jb cleanupcode`; Android `ApplicationId` segments must be valid Java identifiers (`public` is a keyword → `com.focustemplate.mobile`); `DisplayAlert` is obsolete in net11 MAUI → `DisplayAlertAsync`.
- **Not yet verified: an actual on-device/emulator run** (needs a running Android emulator + devtunnel CLI login). Convention going forward: **`AutomationId` on every control** — the native `data-testid`; candidate for a 2.7 architecture/analyzer check.

### 8.4 Mobile testing tiers — M ❓
- **Tier 1 (immediate):** `Public.Api.IntegrationTests` — full coverage of the vertical's logic, no device involved.
- **Tier 2 (thin E2E):** `Public.Mobile.E2E` *(Android ✔ 2026-07-16)* — Appium UIAutomator2 + xUnit (the [dotnet/maui pattern](https://github.com/dotnet/maui/wiki/UITests) and [official guidance](https://learn.microsoft.com/en-us/dotnet/maui/deployment/ui-testing)); **one smoke flow per feature**, logic stays in Tier 1. As built: the fixture boots the AppHost (`Features:Mobile=false`), bakes `http://localhost:5210` into the APK via `-p:CustomAfterMicrosoftCommonTargets` (instead of the originally sketched `10.0.2.2` alias — `localhost` also works natively on the iOS simulator) and maps it with `adb reverse`; no tunnel, no login. Auto-skips (`Assert.Skip`) without an emulator or Appium (project-local npm devDependencies — one-time `npm ci` in the test project; versions pinned via `package-lock.json`), so plain `dotnet test` stays green everywhere. Learning: MAUI `AutomationId` surfaces as Android **resource-id** (`package:id/<AutomationId>`), locate via `MobileBy.Id` — not `AccessibilityId`. iOS: macOS CI runner or BrowserStack ([MS sample](https://learn.microsoft.com/en-us/samples/dotnet/maui-samples/uitest-browserstack/)) — never local on Windows; the fixture's install/driver/port-mapping seam is the intended extension point. This suite doubles as the **MAUI major-version upgrade regression net**.
- **Agents:** persistent tests run via plain `dotnet test`; interactive device driving via the official [appium-mcp](https://github.com/appium/appium-mcp) (bundles UiAutomator2/XCUITest; no Windows driver — desktop stays Tier 1/manual).
- ❓ **[DeviceRunners](https://github.com/mattleibow/DeviceRunners) spike (time-boxed):** on-device xUnit-v3 middle tier (`dotnet test` integration, XHarness CI runner) for platform-dependent code below the UI. Caveat: `0.1.0-preview`, personal repo, activity stalled — verify liveness at adoption; fallback is driving XHarness directly.

### Risks *(re-check at every Aspire/MAUI bump)*
- `Aspire.Hosting.Maui` is **preview**; VS 2026 integration explicitly incomplete. Same lockstep-with-AppHost-SDK rule as `Aspire.Hosting.Blazor`/`.EntityFrameworkCore`.
- **MAUI quality, 2026:** Android 16 edge-to-edge fallout + regression-prone service releases ([community thread](https://github.com/dotnet/maui/discussions/34171)); .NET 11 previews inherit this. Pin MAUI workload versions (`global.json`/CI); expect step 8.3 to absorb churn — sequencing keeps it off the template's core.
- **[MAUI support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/maui):** each major supported only ~6 months after its successor ships, even on LTS .NET → forced annual mobile upgrades. Mitigation = the Tier-2 smoke suite.

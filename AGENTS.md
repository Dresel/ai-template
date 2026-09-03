# Argus

A personal, single-user **crypto-intelligence system** on .NET 11 + Aspire (forked from the
FocusTemplate template): a headless chain-ingestion worker feeding a shared Postgres domain, with a
Blazor WebAssembly dashboard served through a thin BFF over an internal API. End-to-end
OpenTelemetry, Umami analytics, and Playwright E2E tests. Design doc + roadmap:
`shitcoin-trader.md`.

## Projects

- **Argus.AppHost** - Aspire orchestrator. Wires Postgres + migrations, the API, the BFF, the
  ingest worker (behind `Features:Intel`), an optional nginx TLS ingress, and optional Umami analytics.
- **Argus.Data** - EF Core data layer: `AppDbContext`, entities, the `Migrations/` folder,
  a design-time factory, and the dev seed. One domain, referenced by the API, the worker, and the
  AppHost migration resource.
- **Argus.ServiceDefaults** - server-side Aspire defaults (OTel, service discovery, health).
- **Argus.Api** - internal minimal API; never exposed to the browser directly. Reads/writes
  through EF Core (`AppDbContext`), backed by PostgreSQL.
- **Argus.Web** - the Blazor WASM dashboard (Blazorise Material UI).
- **Argus.Web.Bff** - thin YARP BFF: serves the WASM app and proxies `/_api/*` → API,
  `/_otlp/*` → dashboard, `/_analytics/*` → Umami. The browser only ever talks to the BFF.
- **Argus.Web.ClientServiceDefaults** - client-side OTel and shared WASM extensions.
- **Argus.Shared** - DTOs shared between the server and the WASM client.
- **Argus.Worker** (behind `Features:Intel`, default off) - headless chain-ingestion worker
  (Nethereum): polls EVM chains for contract deployments (`TokenDeployment` rows), with persisted
  per-chain cursors (`ChainCursor`), bounded catch-up, RPC endpoint failover + 429 backoff, two
  ingest modes (`BlockReceipts` = complete, for tests/friendly RPCs; `Logs` = budget mode for
  rate-limited public endpoints), ERC-20 metadata capture, and launchpad attribution via a
  configured factory→name map. Keyed RPC URLs stay in the gitignored `appsettings.local.json`
  (AppHost passes them through as `Intel__Ingest__RpcUrls`).

## Tech stack

.NET 11 (preview) Packages use Central Package Management - see `Directory.Packages.props` for versions.

- **Orchestration (Aspire)** - `Aspire.AppHost.Sdk`; `Aspire.Hosting.Blazor` (WASM hosted-model
  service/telemetry proxying, experimental preview); `CommunityToolkit.Aspire.Hosting.Umami`
  (Umami analytics container + its Postgres backend).
- **BFF / gateway** - `Yarp.ReverseProxy`; `Microsoft.Extensions.ServiceDiscovery.Yarp` (resolves
  proxy destinations); `Microsoft.AspNetCore.Components.WebAssembly.Server` (serves the WASM client).
- **Blazor WASM client** - `Microsoft.AspNetCore.Components.WebAssembly`; `Blazorise.Material` +
  `Blazorise.Icons.Material` (Material 3 UI).
- **API** - `Microsoft.AspNetCore.OpenApi`; `Microsoft.OpenApi` pinned above a vulnerable transitive.
- **Data / EF Core** - `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` (client integration:
  `AddNpgsqlDbContext`, health checks, OTel); `Npgsql.EntityFrameworkCore.PostgreSQL` provider;
  `Microsoft.EntityFrameworkCore.Design` (design-time, tooling); the whole EF stack is pinned to one
  version in `Directory.Packages.props`. The `dotnet-ef` CLI is a repo-local tool
  (`.config/dotnet-tools.json`). AppHost wiring uses `Aspire.Hosting.PostgreSQL` +
  `Aspire.Hosting.EntityFrameworkCore` (`AddEFMigrations`).
- **Chain ingest (worker)** - `Nethereum.Web3`; `Newtonsoft.Json` pinned above Nethereum's
  vulnerable transitive.
- **Testing (data)** - `Testcontainers.PostgreSql` - real Postgres per integration-test assembly;
  plain `Testcontainers` for the Anvil (Foundry) EVM container; `Nethereum.StandardTokenEIP20` as
  the reference ERC-20 the ingest tests deploy.
- **Shared infra (ServiceDefaults)** - `Microsoft.Extensions.ServiceDiscovery`;
  `Microsoft.Extensions.Http.Resilience` (Polly-based).
- **Observability** - `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`,
  and instrumentation for AspNetCore / Http / Runtime.
- **Testing** - `Aspire.Hosting.Testing`; `Microsoft.AspNetCore.Mvc.Testing`; `xunit.v3`;
  `Microsoft.Playwright.Xunit.v3` (E2E); `Microsoft.NET.Test.Sdk`.
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

- `dotnet build Argus.slnx`
- `dotnet test Argus.slnx` - xUnit integration tests + Playwright E2E through the BFF
- `dotnet format <project>` - analyzers are strict (StyleCop + IDE rules as errors). Files written
  by tooling usually need this to fix line endings (CRLF, no final newline).
- `dotnet jb cleanupcode Argus.slnx --profile="Built-in: Reformat Code" --include=<path>` -
  ReSharper formatting (repo-local tool, `dotnet tool restore` after a fresh clone; honors
  `.editorconfig` + `Argus.sln.DotSettings`). Prefer scoping with `--include` - a bare run
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
| Any .NET package API question (Blazorise, YARP, OTel, Nethereum, …) | `dotnet-inspect` skill: `dnx dotnet-inspect -y -- member/type/find/diff --package <id>` |
| Browser reproduction, manual UI checks, screenshots | `playwright-cli` skill (persistent E2E tests go in `Argus.Web.E2E`) |
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
     `Argus.Api.IntegrationTests`. A real Postgres runs via Testcontainers (assembly fixture,
     migrations applied once); tests arrange rows against an empty schema. See **Integration test
     database** below for the reset/isolation contract. Chain-ingest behavior →
     `Argus.Worker.IntegrationTests`: same Postgres pattern plus an **Anvil (Foundry)
     container** (a real local EVM chain) - tests deploy real contracts and assert the resulting
     rows.
   - **Aspire system** (cross-resource: BFF↔API, ingress, service discovery, startup order,
     scale-out, telemetry) → `Argus.Web.E2E` (boots the AppHost via
     `DistributedApplicationTestingBuilder`).
   - **UI** (DOM, input, caret, focus, keyboard, routing, client validation, user flow) →
     `Argus.Web.E2E` Playwright, through the BFF.
   - **Unit** (pure logic) → add a unit project when such logic first appears; none today.
4. **Watch it fail** - stop the AppHost (bin lock), then `dotnet test Argus.slnx --filter <name>`.
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

- **Feature flags**: `Features:Analytics`, `Features:TlsOffloadingIngress`, and `Features:Intel`
  (default **off**: no chain listener, no data volume) in the AppHost's `appsettings.json`,
  overridable per-developer via the gitignored `appsettings.local.json`. The E2E fixture pins them
  via CLI args.
- **Shared DTOs** go in `Argus.Shared`. Entities (`Argus.Data`) stay server-side; map
  entity => DTO in the API endpoint, never expose entities to the client.
- **Keep `data-testid` attributes** - the Playwright E2E suite selects on them.

### Database & migrations

- **Schema is applied by the `migrations` resource, never by the API.** The API only reads/writes;
  it `WaitForCompletion`s the migration resource. This is safe under scale-out (no startup migration
  race). Locally/E2E the resource runs `dotnet ef database update` on start; `aspire publish` emits it
  as an idempotent migration-bundle container (a one-shot Job/`restart:no` per compute target).
- **Add a migration** (stop the AppHost first - bin lock):
  `dotnet dotnet-ef migrations add <Name> --project src/Argus.Data --startup-project src/Argus.Data`.
  The design-time `AppDbContextFactory` needs no live DB for this. Migration files land in
  `src/Argus.Data/Migrations/` and are exempt from StyleCop via an `.editorconfig`
  `generated_code` carve-out. You can also use the migration resource's dashboard commands
  (Add Migration, Update/Reset/Drop Database, Status).
- **Dev seed data** lives in `WeatherSeed` and runs via EF `UseSeeding`/`UseAsyncSeeding` when the
  migration tool applies migrations in **run mode only** (the AppHost sets `Database__SeedTestData` on
  the tool resource via `configureToolResource`; the published bundle never seeds). The seed is
  idempotent (insert-if-empty). Implement **both** the sync and async seed delegates - the EF CLI uses
  the synchronous one. Integration tests deliberately run against an unseeded schema.
- **No data volume** on the dev Postgres: each `aspire start` / E2E run gets a fresh, re-seeded
  database, keeping runs deterministic and hermetic. **Exception:** with `Features:Intel` enabled
  the Postgres gets a data volume - the intel archive (deployments, cursors, entity graph) must
  survive restarts.

### Integration test database

Isolation model: **one container, one database per test class, fresh state per test.**

- `PostgresFixture` (assembly fixture) starts **one** Postgres container and hands out databases on
  demand (`CreateDatabaseAsync`). Only `CREATE DATABASE` is serialized (concurrent creations contend
  on the template database); migrations run in parallel.
- `ApiFixture` (class fixture - xunit creates one instance **per test class**) provisions its own
  GUID-named database, migrates it, and points the API at it. Test classes therefore share nothing
  and run **in parallel** - no xUnit collection needed.
- **Test classes derive from `ApiTestBase`**: before every test it resets the class database via
  `ApiFixture.ResetAsync()` ([Respawn](https://github.com/jbogard/Respawn), FK-safe,
  `__EFMigrationsHistory` preserved so migrations never re-run). Every test starts on an empty,
  migrated schema and **arranges exactly the rows it asserts** (`Factory.CreateDbContext()`).

Tests never rely on the dev seed - `WeatherSeed` is dev/E2E-only, and the E2E suite verifies it
through the production seeding path. If a read-heavy suite over an expensive shared dataset emerges
later, add a **seeded, immutable, shared** database + fixture for those tests (seed once, read in
parallel, never mutate).

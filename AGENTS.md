# FocusTemplate

An AI-first .NET 11 template on Aspire, split into two audience verticals: **Admin** (a Blazor
WebAssembly client served through a thin BFF over an internal API) and **Public** (a deliberately
exposed API + a native .NET MAUI mobile client). End-to-end OpenTelemetry, Umami analytics, and
Playwright E2E tests. Both APIs are **spec-first**: one `api.tsp` per vertical (`src/<vertical>/spec/`) generates the endpoints,
Mediator request records, wire models, typed clients and the OpenAPI document. Every consuming project generates
its own slice into its `generated/` folder.

## Projects

Shared spine:

- **FocusTemplate.AppHost** - the Aspire orchestrator, see [apphost](docs/apphost.md).
- **FocusTemplate.ServiceDefaults** - server-side Aspire defaults (OTel, service discovery, health), see
  [apphost](docs/apphost.md).
- **FocusTemplate.Primitives** - the typed ids and permissions every vertical shares, see [spec-first](docs/spec-first.md).
- **FocusTemplate.Data** - the entities, their EF Core mapping, the migrations and the dev seed, see
  [database](docs/database.md).
- **`@spatialfocus/typespec-http-csharp-slim`** - the TypeSpec emitter, consumed as the tgz under `.npm/`, see
  [spec-first](docs/spec-first.md).

Admin vertical (`src/admin/`):

- **FocusTemplate.Admin.Api** - the internal minimal API, see [architecture](docs/architecture.md).
- **FocusTemplate.Admin.Client** - the generated typed HTTP clients of the vertical.
- **FocusTemplate.Admin.Web** - the Blazor WASM client on Radzen, see [web pages](docs/web-pages.md).
- **FocusTemplate.Admin.Web.Bff** - the YARP BFF that serves the client and holds the session, see
  [authentication](docs/authentication.md).
- **FocusTemplate.Admin.Web.ClientServiceDefaults** - client-side OTel and shared WASM extensions.
- **FocusTemplate.Admin.Shared** - the wire contract shared by server and client.

Public vertical (`src/public/`):

- **FocusTemplate.Public.Api** - the exposed API for the mobile client, see [architecture](docs/architecture.md).
- **FocusTemplate.Public.Client** - the generated typed HTTP clients of the vertical.
- **FocusTemplate.Public.Mobile**, **FocusTemplate.Public.Mobile.ServiceDefaults** - the native MAUI app, see
  [mobile](docs/mobile.md).
- **FocusTemplate.Public.Shared** - the mobile wire contract.

## Tech stack

.NET 11 preview with Central Package Management (`Directory.Packages.props`). StyleCop and the IDE rules are build
errors. Each area lists its packages in its doc.

## Running

- `aspire start` (detaches), `aspire stop`, `aspire wait <resource>`. The Aspire CLI starts everything, `dotnet run`
  does not.
- Open the BFF at `http://localhost:5770`, or `https://localhost:7770` through the ingress, and log in as `developer` /
  `developer` (with `Features:LocalKeycloak` off, your own account in the external realm). Keycloak's admin console is
  the `keycloak` resource's endpoint, user `admin`, password in the AppHost's secret store under
  `Parameters:keycloak-password`.
- The launch profile keeps `localhost` URLs: the Hot Reload refresh servers of Visual Studio and `dotnet watch` accept
  only `localhost` origins (CVE-2026-58649), so a page opened through a `*.dev.localhost` link silently loses Hot Reload.
  Hot Reload of the WASM client works with the AppHost started from Visual Studio (Start Without Debugging).
  `aspire start` and `dotnet watch` rebuild and restart the BFF, after which the browser needs F5.
- A running AppHost locks the BFF's binaries: `dotnet build` fails (MSB3027) until it is stopped. A code change reaches
  a running resource through its `rebuild` command (dashboard or Aspire MCP).

## Build, test, format

- `npm run gen` at the repository root (after a one-time `npm ci`), after every change under `src/*/spec/` and every
  emitter bump: one `tsp compile` per `tspconfig.yaml`. The `generated/` folders and `openapi.yaml` are checked in and go
  into the same commit as the spec change. `tsp-output/` is disposable.
- `dotnet build FocusTemplate.slnx` builds, `dotnet test FocusTemplate.slnx` runs unit, integration and E2E tests.
- `dotnet test` runs on Microsoft.Testing.Platform (`global.json`). One test:
  `dotnet test --project tests/<Project> --filter-method "*.TestName"` (repeatable). `--output Detailed` also prints the
  tests that pass. VSTest options such as `--logger` or `--collect` exit with code 5 ("Handshake failures", "Zero tests
  ran").
- Unit, integration and architecture tests also run while the AppHost runs: `--artifacts-path <folder>` builds away
  from the binaries it locks. The E2E suite boots an AppHost of its own and needs the developer's stopped.
- `--no-build` after a failed build runs the binaries of the last successful one and can report green. A run counts only
  after a green build of the same code.
- `dotnet format <project>` fixes what the analyzers reject. Files written by tools usually need it for their line
  endings (CRLF, no final newline). **Never on `FocusTemplate.Public.Mobile`**: it processes each target framework as a
  project of its own and writes conflict markers into shared files. There, and wherever a whole-file reformat is
  wanted, `dotnet jb cleanupcode FocusTemplate.slnx --profile="Built-in: Reformat Code" --include=<path>` (ReSharper,
  a repo-local tool). Without `--include` it reformats the whole solution.
- Edit source files with the editor tools: `sed -i` leaves bare LF line endings, which the analyzers reject as IDE0055.
- Git Bash rewrites arguments that start with `/` before passing them to native programs (`git grep '///'` searched for
  `//`). Prefixing such a command with `MSYS_NO_PATHCONV=1` prevents it.

## Agent toolbox

Check the current Aspire docs before designing or changing anything Aspire-related (`aspire docs search`, MCP
`search_docs`): model knowledge is stale, as the hand-rolled EF migration worker that `AddEFMigrations` replaced shows.
For any other package's API, ask `dotnet-inspect` instead of guessing.

| Need | Use |
|------|-----|
| Start, stop, wait, file locks, port conflicts | `aspire-orchestration` skill (the `aspire` skill routes to all Aspire skills) |
| Logs, traces, resource state of a running app | `aspire-monitoring` skill, Aspire MCP `list_resources`, `list_console_logs`, `list_structured_logs`, `list_traces` |
| Apply a code change to a running resource | Aspire MCP `execute_resource_command` → `rebuild` |
| Aspire API and workflow questions | `aspire docs search/get`, `aspire docs api search --language csharp` |
| Publish and deploy | `aspire-deployment` skill |
| A .NET package's API (Radzen, YARP, OTel, …) | `dotnet-inspect` skill (`dnx dotnet-inspect -y -- skill` prints the guide of the installed tool) |
| Browser reproduction, UI checks, screenshots | `playwright-cli` skill |
| Drive the Android emulator | `appium` MCP, enabled per session (see [mobile](docs/mobile.md)) |
| Change an API (route, wire model, status code, operation) | the spec in `src/<vertical>/spec/` (a new slice is a kebab-case folder imported in `api.tsp`, new permissions go to `src/spec/permissions/`) → `npm run gen` → the handler in the Api project's `Features/` → the consumers → tests, see [spec-first](docs/spec-first.md) |

The skills in `.claude/skills/` and `.agents/skills/` are written by `aspire agent init`, see **Updating Aspire** in
[apphost](docs/apphost.md).

## Development loop

Test-first: a bug is reproduced, a feature specified, by a failing test at the smallest level that fits, before any
production code changes.

- **Integration** (one service: DI, middleware, serialization, auth, EF Core and SQL) →
  `FocusTemplate.Admin.Api.IntegrationTests`, see [integration tests](docs/integration-tests.md).
- **Aspire system** (across resources: BFF and API, ingress, service discovery, startup order, telemetry) and **UI**
  (DOM, input, caret, focus, keyboard, routing, client validation, user flows) → `FocusTemplate.Admin.Web.E2E`, see
  [web E2E](docs/web-e2e.md).
- **UI (mobile)** (native MAUI flows on the Android emulator) → `FocusTemplate.Public.Mobile.E2E`, see
  [mobile](docs/mobile.md).
- **Convention** (a rule every handler or type must follow) → `FocusTemplate.ArchitectureTests`, see
  [database](docs/database.md).
- **Unit** (no browser, no server: Foundation helpers, forms, view models, components rendered with `HtmlRenderer`) →
  `FocusTemplate.Admin.Web.UnitTests`, see [web forms](docs/web-forms.md).

Watch the test fail for the reported reason, make the smallest fix, re-run the test and its area's tests, and format
new files. The summary names root cause, test, fix, commands run and remaining risks. Commit only when asked.

- **Runtime and distributed bugs** go through Aspire: `aspire start`, `aspire wait <resource>`, evidence from the
  Aspire MCP, `rebuild` or `restart` of a resource in place. No hand-started processes.
- **UI bugs** are reproduced with the `playwright-cli` skill, see [web E2E](docs/web-e2e.md). **Mobile bugs and
  features** start red in `FocusTemplate.Public.Mobile.E2E`, see [mobile](docs/mobile.md).
- **Scale-out bugs** start with a deterministic test of two BFF instances sharing one session (request 1 to instance A,
  request 2 to B, asserting business behavior), which needs the BFF integration test project the backlog plans. An Aspire
  system test through the ingress comes only when real infrastructure is needed.
- **Permission prompts**: prefer allowlisted MCP tools and bare single commands. How the allowlist matches a command,
  and the traps around workspace trust, are in [claude-config](.claude/rules/claude-config.md).

## Across areas

- An API change goes spec → `npm run gen` → handler → consumers.
- Consumers call API operations through the generated clients and never hand-write those calls. The BFF's own endpoints
  (`/bff/user`, the diagnostics) are not in a spec and have small hand-written clients.
- Nothing under `generated/` or `Migrations/` is edited by hand.
- Comments, AOT readiness and the names of extension classes apply to all C# code, see [csharp](.claude/rules/csharp.md).
- A change that makes a statement in AGENTS.md, `docs/` or `.claude/rules/` wrong corrects it in the same commit.

## Index

A doc explains how an area works and why. A rule lists what to follow. Claude Code loads it when it reads, edits or
writes a file of its area, and other agents read it from here.

| Area | Doc | Rule |
|---|---|---|
| Projects | [architecture](docs/architecture.md) | – |
| AppHost, service defaults, feature flags | [apphost](docs/apphost.md) | [apphost](.claude/rules/apphost.md) |
| TypeSpec, emitter, generated code | [spec-first](docs/spec-first.md) | [spec-first](.claude/rules/spec-first.md) |
| Data, EF Core, migrations, handlers' queries | [database](docs/database.md) | [database](.claude/rules/database.md) |
| Integration tests | [integration tests](docs/integration-tests.md) | [integration-tests](.claude/rules/integration-tests.md) |
| Authentication: Keycloak, BFF, tokens | [authentication](docs/authentication.md) | [authentication](.claude/rules/authentication.md) |
| Authorization: permissions, groups | [authorization](docs/authorization.md) | [authorization](.claude/rules/authorization.md) |
| Web pages and view models | [web pages](docs/web-pages.md) | [web-pages](.claude/rules/web-pages.md) |
| Web forms and validation | [web forms](docs/web-forms.md) | [web-forms](.claude/rules/web-forms.md) |
| Web E2E tests | [web E2E](docs/web-e2e.md) | [web-e2e](.claude/rules/web-e2e.md) |
| Mobile | [mobile](docs/mobile.md) | [mobile](.claude/rules/mobile.md) |
| C# conventions | – | [csharp](.claude/rules/csharp.md) |
| Packages | – | [packages](.claude/rules/packages.md) |
| Claude Code configuration | – | [claude-config](.claude/rules/claude-config.md) |

Planning notes may sit untracked at the repository root (`backlog.md`, `lamama.md`, …). They are personal.

Cross-project and operational knowledge lives in the wiki at docs.spatial-focus.net, reached through the
`spatial-focus-docs` MCP server from `.mcp.json` (one sign-in per developer, reads allowlisted, writes asking first):

- [Infrastructure – Überblick](https://docs.spatial-focus.net/doc/infrastructure-uberblick-EnrCjXS1oh): the two
  clusters, everything deployed through Flux from `SpatialFocus/Infrastructure`
- [Flux CD (Gitops)](https://docs.spatial-focus.net/doc/flux-cd-gitops-K545MSEJBy): the GitOps setup and the image
  automation from `focus.azurecr.io`
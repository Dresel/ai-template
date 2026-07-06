# FocusTemplate

An AI-first .NET 11 template on Aspire: a Blazor WebAssembly client served through a thin BFF,
a backend API, end-to-end OpenTelemetry, Umami analytics, and Playwright E2E tests.

## Projects

- **FocusTemplate.AppHost** - Aspire orchestrator. Wires the API, BFF, an optional nginx TLS
  ingress, and optional Umami analytics.
- **FocusTemplate.Api** - internal minimal API; never exposed to the browser directly.
- **FocusTemplate.Web** - the Blazor WASM client (Blazorise Material UI).
- **FocusTemplate.Web.Bff** - thin YARP BFF: serves the WASM app and proxies `/_api/*` → API,
  `/_otlp/*` → dashboard, `/_analytics/*` → Umami. The browser only ever talks to the BFF.
- **FocusTemplate.Web.ClientServiceDefaults** - client-side OTel and shared WASM extensions.
- **FocusTemplate.ServiceDefaults** - server-side Aspire defaults (OTel, service discovery, health).
- **FocusTemplate.Shared** - DTOs shared between the server and the WASM client.

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
- **Shared infra (ServiceDefaults)** - `Microsoft.Extensions.ServiceDiscovery`;
  `Microsoft.Extensions.Http.Resilience` (Polly-based).
- **Observability** - `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`,
  and instrumentation for AspNetCore / Http / Runtime.
- **Testing** - `Aspire.Hosting.Testing`; `Microsoft.AspNetCore.Mvc.Testing`; `xunit.v3`;
  `Microsoft.Playwright.Xunit.v3` (E2E); `Microsoft.NET.Test.Sdk`.
- **Analyzers** - `StyleCop.Analyzers` (global, enforced as build errors).

## Running

Use the Aspire CLI, not `dotnet run`:

- `aspire start --apphost src/FocusTemplate.AppHost/FocusTemplate.AppHost.csproj` (detaches)
- `aspire stop --apphost <same>`
- `aspire wait <resource> --apphost <same>` - block until healthy

**Stop the AppHost before `dotnet build`/`dotnet test`** - a running BFF locks its output binary and
the build fails (MSB3027). To apply code changes without a full restart, use the **rebuild** command
on the affected resource (Aspire dashboard or MCP).

## Build, test, format

- `dotnet build FocusTemplate.slnx`
- `dotnet test FocusTemplate.slnx` - xUnit integration tests + Playwright E2E through the BFF
- `dotnet format <project>` - analyzers are strict (StyleCop + IDE rules as errors). Files written
  by tooling usually need this to fix line endings (CRLF, no final newline).
- `dotnet jb cleanupcode FocusTemplate.slnx --profile="Built-in: Reformat Code" --include=<path>` -
  ReSharper formatting (repo-local tool, `dotnet tool restore` after a fresh clone; honors
  `.editorconfig` + `FocusTemplate.sln.DotSettings`). Prefer scoping with `--include` - a bare run
  reformats the whole solution.

## Agent toolbox

Skills live in `.claude/skills/`. Pick by task - these are all permission-allowlisted:

| Need | Use |
|------|-----|
| Operate the app: start/stop/wait, resource status | `aspire` skill → Aspire CLI (`aspire start/stop/wait/describe`) |
| Runtime evidence from a running app: logs, traces, resources | Aspire MCP (`list_resources`, `list_console_logs`, `list_structured_logs`, `list_traces`) |
| Apply a code change to a running resource | Aspire MCP `execute_resource_command` → `rebuild` (no full restart) |
| Aspire API / workflow questions | `aspire docs search/get`, `aspire docs api search --language csharp` (or MCP `search_docs`/`get_doc`) |
| Any .NET package API question (Blazorise, YARP, OTel, …) | `dotnet-inspect` skill: `dnx dotnet-inspect -y -- member/type/find/diff --package <id>` |
| Browser reproduction, manual UI checks, screenshots | `playwright-cli` skill (persistent E2E tests go in `FocusTemplate.Web.E2E`) |
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
   - **Integration** (one service: DI, middleware, serialization, auth, framework) →
     `FocusTemplate.Api.IntegrationTests`.
   - **Aspire system** (cross-resource: BFF↔API, ingress, service discovery, startup order,
     scale-out, telemetry) → `FocusTemplate.Web.E2E` (boots the AppHost via
     `DistributedApplicationTestingBuilder`).
   - **UI** (DOM, input, caret, focus, keyboard, routing, client validation, user flow) →
     `FocusTemplate.Web.E2E` Playwright, through the BFF.
   - **Unit** (pure logic) → add a unit project when such logic first appears; none today.
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

**Scale-out bugs:** first a deterministic integration test with two BFF instances sharing one session
(req 1 → instance A, req 2 → instance B; assert business behavior, not infra). Escalate to an Aspire
system test through the nginx ingress only if real infra is required, using logs/traces to find the
failing resource.

Prefer already-permitted MCP tools / skills and **bare single commands** (no `&&`/pipes/`cd`) so the
loop runs without permission prompts.

## Conventions

- **Feature flags**: `Features:Analytics` and `Features:TlsOffloadingIngress` in the AppHost's
  `appsettings.json`, overridable per-developer via the gitignored `appsettings.local.json`. The
  E2E fixture pins them via CLI args.
- **Shared DTOs** go in `FocusTemplate.Shared`.
- **Keep `data-testid` attributes** - the Playwright E2E suite selects on them.

# Lamama: fork of the template, first plan

*Written 2026-10-07, nothing started. Sources: this template (ai-template), the release PoC
(`C:\Users\c\Projects\lamama-release-poc`: README, PRODUCT.md, DESIGN.md, docs/preliminary-dashboard.md) and the Outline
page "Release Dashboard – Architektur und offene Entscheidungen" (Landmanager › Lamama). This file is the hand-over for
the session that does the fork.*

## Goal

Lamama, the internal tool for Landmanager, starts as a copy of this template in a repository of its own. It keeps the
Admin vertical with the user-management slice, drops the demos and the Public vertical, and gets **Releases** as its
first new slice: the release dashboard the PoC prepared. The template is not finished and keeps evolving, so the fork
must be able to take later template changes.

Why the template fits: the PoC decided on a Blazor WASM client, an ASP.NET Core API, PostgreSQL, Keycloak and Radzen,
which is the template's stack. Two open points of the PoC are answered by the fork:

- *Where Lamama is built*: here. The planned split into a Razor class library plus a thin demo host is not needed; the
  Releases pages and endpoints live in `Admin.Web` and `Admin.Api` like any slice.
- *User roles and permissions*, which PRODUCT.md lists as not yet assigned to a module: the user-management slice, with
  permissions granted to groups.

## Decisions to take first

| # | Question | Recommendation |
|---|---|---|
| 1 | Repository and history | A fresh repository whose first commit names the template commit it was forked from, with the template added as a remote. Template changes are diffed from that commit and ported. Keeping the history buys little: the rename and the removals would make every merge of a template commit conflict. |
| 2 | Names | `Lamama.*` throughout, the vertical keeps its name: `Lamama.AppHost`, `Lamama.Data`, `Lamama.Primitives`, `Lamama.Admin.Api`, `Lamama.Admin.Web`, `Lamama.Admin.Web.Bff`, … A public website can come back as `src/public/`. |
| 3 | Permissions per environment | The template's model: groups in Lamama grant `ReleasesPermissions.ReleaseTest` and so on. The architecture page says "roles per environment as policies" in Keycloak; groups are the same idea, managed by the user-management slice. |
| 4 | Demo scope | Frontend-only releases against the sandbox (dev2, test2, staging2 in the Infrastructure repository), where the CLI already works. Release sets with core components only in simulation, since the sandbox has no backend. |
| 5 | UI language | German, like the click dummy, for the whole app: the fork sets `Cultures.Default` to de. The template already speaks en and de everywhere, its pages, FluentValidation's messages, the generated field names and the API's answers (see `docs/localization.md`). |

## Step 1: fork and strip

Goal: a green build, green tests and a working `aspire start` with nothing but the shell and user management.

- **Public vertical**: `src/public/` (Api, Client, Shared, Mobile, Mobile.ServiceDefaults, the spec),
  `tests/FocusTemplate.Public.Mobile.E2E`, the MAUI device resources and the Dev Tunnel in the AppHost, `Features:Mobile`,
  the MAUI, DevTunnels and Maui.Core entries in `Directory.Packages.props`. The build no longer needs the MAUI workloads.
- **Validation demo**: `src/admin/spec/demo-profiles/`, `Features/DemoProfiles/` in Api and Web, `DemoProfileCustomRules`
  in `Admin.Shared`, the six `Validation*Tests` of the E2E suite and the demo's unit tests. `Foundation/Forms/` and its
  unit tests (which use their own test form) stay.
- **Weather**: the `weather-forecasts` spec, both `Features/WeatherForecasts/`, the entities `WeatherForecast`, `Station`,
  `Observation`, `Alert` with their configurations and enums (`MapEnum` calls), `WeatherSeed`, the typed ids
  `WeatherForecastId`, `StationId`, `ObservationId`, `AlertId`, `WeatherTests` and the weather integration tests. PostGIS
  goes too: back to the plain Postgres image (AppHost and Testcontainers) and no NetTopologySuite.
- **Migrations**: squashed into one fresh `InitialCreate`.
- **Docs and rules**: the Public vertical (`docs/architecture.md`, the `mobile` doc and rule), `docs/web-forms.md` (rewritten around the Groups form; the
  async server rule and list rows lose their example until a Releases form needs them), the stations parts of the data
  sections, and every mention of the removed tests.
- Check `PersistentDatabaseTests` and the AppHost seed path, which may lean on the weather seed.

Then the rename (decision 2), then the emitter's `tspconfig.yaml` namespaces, which follow the project names.

## Step 2: shell and design

- **Radzen 12**: the click dummy runs 12.0.1, the template 11.5.1. Bump, then check the wrappers in `Foundation/Forms/`
  (attributes on `RadzenTextBox`, `InputAttributes` on `RadzenNumeric`, `TooltipService`, `DialogService`).
- **Design system**: DESIGN.md, `.impeccable/` and the click dummy's `app.css` (about 2,000 lines: Radzen's software
  theme recoloured to the Landmanager green, the "cadastral sheet") move in beside the template's form styles. The
  template's dark-theme toggle goes unless the design gets a dark variant.
- **Shell**: "Lamama · Releases" header with the module switcher of the click dummy (Releases, Benutzerverwaltung, the
  planned modules greyed out: Mitteilungen, Monitoring, Kunden, Datenschutz, Protokoll), on top of the template's
  `MainLayout` with login, logout and the permission-driven menu.
- **German texts** (decision 5), including `PermissionLabel` and the user-management pages.

## Step 3: the release library moves in

```
src/
  Lamama.ReleaseManagement/             # the PoC library, no Lamama knowledge
  Lamama.ReleaseManagement.Simulation/  # the simulated world, see below
  admin/Lamama.Admin.Api/Features/Releases/
tests/
  Lamama.ReleaseManagement.Tests/
tools/
  Lamama.ReleaseCli/                    # the PoC CLI, for runs outside the dashboard
```

- The library stays a separate assembly in the shared spine, with the PoC rule: it knows nothing about Lamama, EF Core,
  Mediator or configuration; paths, marker keys, hosts and commit messages come from the caller. An architecture test
  checks its references.
- net10 → net11, and the template's conventions, which the PoC follows only in part. Mechanical:
  - no copyright headers (on all 132 PoC files), usings outside the namespace, UTF-8 without BOM
  - no `///` on hand-written code (110 files): short `//` comments for the why, the long explanations go into the
    library's README
  - Central Package Management, lock files, pinned StyleCop (the PoC floats `*-*`), warnings as errors
- …and three changes in substance (below with the `TimeProvider` and the manual check):
  - **Failures as values.** A step fails by throwing `StepFailedException`, beside a `StepResult` for done, skipped and
    rejected. One result with a failed case matches the template's "errors are values" and what the dashboard
    persists anyway. The runner still catches what clients throw (timeouts, network).
  - **The desired revision as a result.** Commit steps leave `DesiredRevision` in the mutable `ReleaseContext` for the
    next wait step to find. Returned in the commit step's result and handed to the wait step, the dependency is explicit,
    and the persisting runner stores it without a side channel.
  - **Data types beside their source.** `Models/` holds every type of the library; `DeploymentStatus` belongs to
    `Kubernetes/`, `CommitInfo` to `Git/`, as the options already do.
- **`TimeProvider` in the wait steps.** They measure with `Stopwatch` and wait with `Task.Delay` today. With a
  `TimeProvider`, tests and the simulation run on `FakeTimeProvider`.
- **Non-blocking manual check.** `ManualCheckStep` blocks on `IReleaseInteraction`. The dashboard needs the state
  "waiting for confirmation" persisted: the step ends the run in that state, an API call answers, and the run resumes at
  that step.
- Octokit and KubernetesClient use reflection, against the template's AOT rule. Server only, so accepted for now; the
  Git Data API is five calls if Octokit has to go.

## Step 4: simulation

Three modes, chosen by a flag such as `Features:ReleaseSimulation`:

| Mode | Where | Writes to | Needs |
|---|---|---|---|
| Simulation (default) | `aspire start`, CI, E2E, demos | the in-memory world | nothing |
| Sandbox (opt-in in `appsettings.local.json`) | a developer working on the integration | the Infrastructure repository, dev2/test2/staging2 | kubeconfig context `satgrass-dev`, GitHub token, Scaleway key in user secrets |
| Production | the deployed Lamama only | LamaInfra (test, staging, prod) | GitHub App, read-only ServiceAccount, Scaleway IAM application |

The sandbox is shared: every developer's Lamama has its own database, so "one release per environment" holds only
within one instance. Releases there need coordination.

**How the simulation works.** Interface fakes over one shared world, no mocking library and no HTTP mocking. The
library reaches everything through `IGitOpsRepository`, `IClusterReader`, `IImageRegistry` and `IHostProbe`; DI registers
the simulated ones when the flag is on. The only write the dashboard ever makes is a commit, so the world follows from
Git and the clock, without explicit state machines:

- **Git**: the PoC's `FakeGitOpsRepository` (files, blob-SHA check, commit graph, compare) plus commit timestamps,
  `GetHistoryAsync`, a lock, and seed files per environment with the real markers.
- **Flux** applies the newest commit older than its delay; the applied manifests are that commit's files.
- **Deployment and pods** follow the image tag of the applied `kustomization.yaml` (`ManifestEditor.ReadImageTag`) and
  switch over a few seconds after the apply.
- **Ingress** follows the maintenance marker (`ReadMarkedValue`).
- **Registry**: a fixed list of tags per image.
- **Failures** are switches on the world: a rollout ends in CrashLoopBackOff, `/version.json` reports the wrong version.

The real steps run unchanged against it: commit, wait for Flux, verify, rollout, smoke test, manual check, revert,
resume after a restart.

**URLs.** Public host, release host and version path per environment become configuration (the click dummy builds
`https://app.{Name}.landmanager.de/` in code). In simulation they point at simulated hosts the API serves from the same
world, so a demo never links to real Landmanager and the manual-check link opens something:

- `/simulation/{env}/app/`: a version page, or the maintenance page with 503 and `Retry-After` while maintenance is
  applied
- `/simulation/{env}/release-host/`: the new version while the release-host marker is on, 404 otherwise
- `/version.json` under both

The probe in simulation is then the real `HttpHostProbe`, which gets tested on the way. Locally the API's port is
reachable; a deployed demo needs a BFF route for `/simulation/*`.

**Limits.** The simulation never runs the real GitHub, Kubernetes and Scaleway adapters; those stay with the sandbox and
with HTTP-level tests per adapter if they get any. The world lives in memory and the releases in the database, so with
`Features:PersistentDatabase` on, an API restart separates them unless the world persists its files.

Unit tests of single steps keep the PoC's scripted fakes.

## Step 5: the Releases slice

**Spec**: `src/admin/spec/releases/` in namespace `Lamama.Admin.Releases`, one file per interface (environments and
the overview, release sets, releases), so the code lands in `Features/Releases/<Interface>/`. The click dummy's
`Models/` are the draft of the contract. Actions are custom methods: `POST /releases/{id}:confirm`, `:reject`,
`:revert`, `POST /release-sets/{id}:promote`. Permissions in `src/spec/permissions/releases.tsp` (decision 3), for
example `ViewReleases`, `PlanSets`, `ReleaseTest`, `ReleaseStaging`, `ReleaseProd`.

**Data**: `ReleaseSet`, `Release`, `ReleaseStep` as plain entities with auditing and a row version on the set.
Environments and components come from configuration first, as in the CLI; the architecture page models them as
entities, which can follow.

**Rules** in the handlers, answered with 409 cases: one release per environment, the proven chain
(`requiresProvenOn`), the hotfix rule (`x.y.z.n` only where `x.y.z` runs), prod confirmation. The click dummy's
`DemoStore` (`Blocker`, `ProvenOn`, `EligibleSets`) is their specification.

**Runner**: a `BackgroundService` in the API that runs one step at a time, persists each step's record and the
`DesiredRevision` after commit steps, and resumes after a restart. One runner per environment at a time, through a
Postgres advisory lock. The Lamama conventions (file paths, markers, commit messages) move from the CLI's
`LamamaRelease.cs` into the slice.

**Live status**: polling while a release runs first, through the template's reload commands. SignalR later, with a hub
route of its own: the `/_api` policy requires the `X-CSRF` header, which a browser cannot send on a WebSocket upgrade.

**UI**: the click dummy's pages and components ported to `ViewModelPage` and the generated clients, markup and CSS kept.

**Stages**, as the architecture page orders them:

1. Overview, read-only: the four truths per component and environment, drift, maintenance state, failures per cell.
2. Maintenance switch for staging and prod, as a release of its own (this brings the runner).
3. Release sets: plan on test, promotion with the proven chain, history, revert.
4. Release host, smoke tests, optional manual check.
5. Hotfix rules, Bitbucket changelog, release notes.

UptimeRobot, Teams and Bitbucket come with their stages, each behind an interface with a recording fake in simulation.

## Outside the code

Needed before Lamama releases anything real:

- GitHub App with `Contents: write` on LamaInfra (and on Infrastructure for the sandbox)
- Scaleway IAM application with `ContainerRegistryReadOnly`
- read-only ServiceAccount with its ClusterRole on dev and prod
- Keycloak client for Lamama
- Teams webhook
- LamaInfra caught up with the sandbox (open on the architecture page): wildcard certificate and DNS per environment,
  maintenance page with 503, the `witty-dune` ingress with its marker, relative redirect URIs in the frontend, the
  Keycloak redirect URI and web origin for the release host
- where Lamama itself is deployed, following the template's Flux pipeline

## Risks

- **Template drift**: the template keeps changing; each port gets harder the longer it waits.
- **Radzen 12** may break the form wrappers.
- **Forms without the demo**: the validation machinery (async rules, debounce, list rows) loses its only consumer until
  a Releases form needs it.
- **Shared sandbox**: two developers releasing to test2 at once collide.
- **Reflection-based clients** in the server, against the AOT rule.

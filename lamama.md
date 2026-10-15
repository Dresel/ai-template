# Lamama and Focuma: plan for the new repository

*Written 2026-10-07, nothing started. Updated 2026-10-13: Releases is a feature package, and the new repository holds
two products, Lamama and Focuma, that share the spine and the shared features. Updated 2026-10-14: user management is
part of the Admin spine, and what every product's hosts run identically lives once in hosting libraries, so a product's
host projects keep composition alone. The shared libraries carry no vertical in their names, user management excepted.
Sources: this template (ai-template),
the release PoC (`C:\Users\c\Projects\lamama-release-poc`: README, PRODUCT.md, DESIGN.md,
docs/preliminary-dashboard.md) and the Outline page "Release Dashboard – Architektur und offene Entscheidungen"
(Landmanager › Lamama). This file is the hand-over for the session that does the fork.*

## Goal

A new repository, copied from this template, holds two products:

- **Lamama**, the internal tool for Landmanager: user management, release management for Landmanager's products,
  monitoring (health jobs), security scans (Trivy) and a public status page.
- **Focuma** ("Focus management"): release management for the Focus projects, Lamama and Focuma itself
  included.

Both keep the template's Admin vertical, drop the demos, the weather feature and the Public vertical with its mobile
app, and share what both need: the spine, user management and **Releases**, the release dashboard the PoC prepared.
Each product has its own solution, AppHost, database and deployment.

The template is not finished and keeps evolving. Its changes are ported into the repository, and what the products
build that every host could use goes back into the template.

Why the template fits: the PoC decided on a Blazor WASM client, an ASP.NET Core API, PostgreSQL, Keycloak and Radzen,
which is the template's stack. Two open points of the PoC are answered:

- *Where it is built*: in the new repository. The PoC's planned Razor class library comes back as the feature's
  `Releases.Web`, and its endpoints as `Releases.Api`, both mounted into each product's host with a few lines.
- *User roles and permissions*, which PRODUCT.md lists as not yet assigned to a module: user management, part of
  the Admin spine (decision 12), with permissions granted to groups.

## Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Repository and history | One repository for both products. Its first commit names the template commit it was forked from, with the template added as a remote. History is not kept: the rename and the removals would make every merge of a template commit conflict. Porting: see **Porting**. |
| 2 | Names | The products as `Lamama.*` and `Focuma.*`, one word each as .NET treats a coined name. Repository, images (`lamama-admin-api`), DNS, realms and Flux paths are lowercase anyway, and a second spelling would only get mistyped. The UI may style them ("LaMaMa", "FocuMa"). The shared parts as `SpatialFocus.*`: `Focus.*` sits too close to `Focuma.*` in every list, collides with members named `Focus` in UI code, and `SpatialFocus` already names the emitter's TypeSpec library (`SpatialFocus.Http`), whose types then carry the same name in spec and C#. The template's spine takes the same prefix, so ports need no renaming there. A shared library names no vertical (`Web.Foundation`, `Api.Hosting`), since an Admin and a public side can both use it. One that serves a single vertical keeps the vertical's name, as `Admin.UserManagement` does, whose users and groups endpoints belong on the internal API alone. `Web.*` means the Blazor WebAssembly client behind the BFF: a server-rendered site such as `Lamama.Public.Web` uses none of it. In prose the prefix is dropped: `Web.Bff.Hosting` means `SpatialFocus.Web.Bff.Hosting`. A feature is named after its owner: a Lamama-only feature is `Lamama.Monitoring.*`, renamed to `SpatialFocus.Monitoring.*` when it moves to the shared folder. Open: the repository's name (`focus-management`, `spatialfocus-management`, …). |
| 3 | Permissions per environment | The template's model: groups grant `ReleasesPermissions.ReleaseTest` and so on. The architecture page says "roles per environment as policies" in Keycloak; groups are the same idea, managed by user management. |
| 4 | Demo scope | Frontend-only releases against the sandbox (dev2, test2, staging2 in the Infrastructure repository), where the CLI already works. Release sets with core components only in simulation, since the sandbox has no backend. |
| 5 | UI language | German, like the click dummy, for Lamama: it sets `Cultures.Default` to de. The template already speaks en and de everywhere, its pages, FluentValidation's messages, the generated field names and the API's answers (see `docs/localization.md`). Focuma the same unless decided otherwise. |
| 6 | Releases | A shared feature both products include. Products are a dimension inside the feature, configured by each host: Lamama manages Landmanager's products (Scaleway registry, maintenance step), Focuma manages the Focus projects, Lamama and itself (ACR registry, no maintenance step). |
| 7 | Slice, product feature or shared feature | A **slice** when its pages and endpoints serve one vertical on the product's own data. A **product feature** (`src/<product>/features/`) when the code has data with its own lifecycle, serves more than one vertical, has jobs of its own, can be switched off or is a likely candidate for the other product. A **shared feature** once both products include it. In doubt a slice, except when one of the first three reasons holds from the start: moving a slice out later is the expensive move. Lamama: monitoring and security are product features; Kunden, Datenschutz, Protokoll and Mitteilungen start as slices. |
| 8 | Public side | Lamama's status page is its Public vertical: `Lamama.Public.Web`, server-rendered, anonymous and cacheable, reading monitoring's read-only context on `focusdb-readonly`. A JSON API comes when something machine-readable needs it. A later public side of either product shares the product's data the same way. |
| 9 | Jobs | A Worker host per product from the start, see **Worker**. |
| 10 | Deploying itself | Focuma deploys Lamama and itself. Readiness-gated rollouts (`maxUnavailable: 0`) keep a version that does not start from replacing the old pods, the runner's steps resume after a restart, and a revert commit in the GitOps repository is the fallback for a version that starts and misbehaves. A runbook in `docs/focuma/` names the commit to revert, where to watch Flux, and how to mark the release as aborted. |
| 11 | Typed ids and permissions | Each package's typed ids and permissions in an assembly of its own, each primitives project writing one namespace (`primitives-namespace`): `SpatialFocus.Primitives` the spine's ids, `Permission` and user management's permissions, a feature's `*.Primitives` its own (`SpatialFocus.Releases.Primitives`, `Lamama.Monitoring.Primitives`). Each product generates its list from a spec that imports the permission sets it includes (`src/lamama/admin/spec/primitives.tsp`, compiled by `Lamama.Admin.Primitives` with `permission-list: AdminPermissions`), so a product's groups page lists only its own. User management takes the list through DI. Done in the template (step 0); `Permission.All` is gone. |
| 12 | User management | Part of the Admin spine: `SpatialFocus.Admin.UserManagement.*` under `src/shared/admin/user-management/`. Every Admin host depends on it, since the BFF asks `/users/me` on each load and every `@requiresPermission` needs its permission handler, which a feature, being optional, could not carry. It manages `SpatialFocus.Data`'s tables and has no data of its own. Moved in the template before the fork (step 0). |
| 13 | Hosting libraries | What every product's hosts run identically lives once in a `SpatialFocus.*.Hosting` library, named after the kind of host it serves: `Api.Hosting`, `Web.Bff.Hosting`, `Web.Hosting`, `AppHost.Hosting`, `Worker.Hosting`. A host calls what it needs: the Admin API all of `Api.Hosting`, a public API `AddApiDefaults()` and the localization without the sign-in. A product's host project keeps its `Program.cs`, configuration, menu and own slices. Hosting is for hosts only: what a feature's code builds on goes into a Foundation. |
| 14 | References | Layered, in chains that meet at the bottom: products → hosting → the foundations, `Localization`, `Names` and the generated libraries; products → user management → the foundations and `Data`; products → the features → the foundations. Hosting references no feature and no vertical. A feature references the foundations alone, which keeps `Data`, user management and hosting out of its reach. `FeatureTests` checks it as an allowlist, so a new shared project stays closed to features until it is added on purpose. `Web.Bff.Hosting` takes the user's permissions from the product's `IUserPermissionSource`, which the product's BFF provides over `Admin.UserManagement.Client`. `ProjectReferenceTests` checks from the project files that a `SpatialFocus.*` project references no product project, and one without a vertical in its name none of a vertical's. Should a public side ever sign users in, `/users/me`, the provisioning and the permission handler move out of user management into a general part. |
| 15 | Migrations | A shared model ships its migrations unless its data differs per product. The spine's `AppDbContext` seeds the product's permission rows and the administrators' grants, so its migrations are the product's: `Lamama.Data` and `Focuma.Data`, each with a `PermissionSeed` model customizer over its `AdminPermissions.All` and `UseProductMigrations()` for what migrates. EF finds the design-time factory in the startup project, so it sits in each Admin API, which the AppHost's migrations resource runs as startup project too. A feature ships its migrations in its data project, and a feature with permissions, added or removed, is a migration in the product's `Data`. A product's own slice keeps its tables in a context of the product's in its `Data` (schema `admin`); a self-contained capability becomes a feature package. Done in the template (step 0). |

## Structure

```
<repo>/
  lamama.slnx                         Lamama + shared
  focuma.slnx                         Focuma + shared
  global.json  Directory.Build.props/.targets  Directory.Packages.props  nuget.config
  .editorconfig  stylecop.json  .config/dotnet-tools.json
  package.json  package-lock.json  .npm/          TypeSpec and the emitter tgz
  scripts/
    gen.mjs                           one tsp compile per C# project's tspconfig.yaml, both products
    port-from-template.mjs            format-patch, rename map, git am -3
  AGENTS.md  CLAUDE.md  .mcp.json
  docs/                               the shared areas (from the template), docs/lamama/, docs/focuma/
  .claude/rules/  .claude/skills/  .agents/skills/
  .github/workflows/                  lamama.yml, focuma.yml: path filters, images to focus.azurecr.io

  src/
    shared/
      spec/                                  SpatialFocus.Spec: primitives.tsp, http.tsp, paging.tsp
      SpatialFocus.Primitives                the spine's typed ids, Permission and user management's permissions
                                             (generated)
      SpatialFocus.Http                      the library types (generated)
      SpatialFocus.ServiceDefaults           OTel, health, service discovery
      SpatialFocus.Names                     DesignTimeNames: what a migrations resource sets and a design-time
                                             factory reads
      SpatialFocus.Data.Foundation           base context, conventions, auditing, ICurrentUser, UserReference
      SpatialFocus.Data                      identity: User, Group, the grants, AppDbContext, the dev seed. Its
                                             migrations are the product's (decision 15)
      SpatialFocus.Localization              for any API and client: ConventionBasedStringLocalizerFactory,
                                             CustomLanguageManager with its German texts, ValidationTexts,
                                             AddResxLocalization()
      SpatialFocus.Server.Foundation         web-free: the backplane's publishing half, the job-loop base (with the
                                             first job, see Worker)
      SpatialFocus.Api.Foundation            event streams, the backplane's receiving half, the token's claims
      SpatialFocus.Web.Foundation            Blazor: pages, forms with their stylesheet, feedback, appearance, cultures
      SpatialFocus.Web.ClientServiceDefaults client OTel, analytics, proxied clients, CSRF, NavigateToLogin
      SpatialFocus.Api.Hosting               AddApiDefaults(), AddSignalBackplane(redis), Keycloak bearer, the
                                             current user, AddApiLocalization(cultures), chaos
      SpatialFocus.Web.Bff.Hosting           the BFF: session, OIDC with PAR, /bff/*, YARP transforms, client
                                             configuration, the OTLP trace filters, AddApiClient<T>(). The session
                                             cookie's name and the API's address come from the product, the user's
                                             permissions from its IUserPermissionSource.
      SpatialFocus.Web.Bff.Shared            the BFF's answers to the client: /bff/user (UserInfoResponse,
                                             UserClaim), BffClaimTypes, AnalyticsConfiguration
      SpatialFocus.Web.Hosting               the session as auth state, AppRouter, Forbidden, the header's account
                                             part, the loading screen's CSS
      SpatialFocus.AppHost.Hosting           Keycloak realm, TLS-offloading ingress, AddMigrations(…) with the
                                             context's name as a string, Umami, local settings
      SpatialFocus.Worker.Hosting            the heartbeat, the registration of the job loops

      admin/
        user-management/                     the one shared part with the vertical in its name (decision 2)
          spec/                              api.tsp, users.tsp, groups.tsp, permissions.tsp
          SpatialFocus.Admin.UserManagement.Shared   wire contract (generated), contract resx
          SpatialFocus.Admin.UserManagement.Client   typed clients (generated)
          SpatialFocus.Admin.UserManagement.Api      users and groups, /users/me, provisioning, the permission
                                                     handler, UserPermissions, the administrator bootstrap, GroupChanged
          SpatialFocus.Admin.UserManagement.Web      pages, member picker, permission labels
          SpatialFocus.Admin.UserManagement.Tests    its view models and texts
          SpatialFocus.Admin.UserManagement.IntegrationTests   users, groups, permissions, provisioning, the
                                                     bootstrap, on the shared test host

      features/
        releases/                            spec, Primitives, Shared, Client, Data, Api, Jobs, Web, and the
                                             tests beside them: Tests, IntegrationTests on the shared test host,
                                             E2E (abstract classes each product's E2E suite runs)
          SpatialFocus.ReleaseManagement  SpatialFocus.ReleaseManagement.Simulation
          SpatialFocus.ReleaseManagement.Tests   the library and the simulation
    lamama/
      Lamama.AppHost                  resource graph, the template's ports, Keycloak on 8080, realm lamama
      Lamama.Names
      Lamama.Data                     the migrations of the spine's AppDbContext, PermissionSeed over
                                      AdminPermissions.All, UseProductMigrations(); later the context of own slices
      Lamama.Worker                   Program.cs: release runner, health checks, scans
      admin/
        spec/                         primitives.tsp: the permission sets Lamama includes (+ api.tsp once
                                      Lamama has API slices of its own)
        Lamama.Admin.Primitives       AdminPermissions (generated), later the ids of own slices
        Lamama.Admin.Api              Program.cs, appsettings, own slices, the design-time factory of the spine's
                                      context
        Lamama.Admin.Web.Bff          Program.cs, appsettings (proxy routes, session cookie
                                      __Host-lamama.session, the API's address), UserPermissionSource, the
                                      reference to Lamama.Admin.Web
        Lamama.Admin.Web              Program.cs, App.razor, MainLayout (title, menu), Home, NotFound,
                                      index.html, logo
        Lamama.Admin.Shared           Cultures (de first), later the contracts of own slices
                                      (+ Client once Lamama has API slices of its own: after step 1 it has
                                      none, see there)
      public/
        Lamama.Public.Web             the status page
      features/
        monitoring/                   Lamama.Monitoring.*: spec, Primitives, Shared, Client, Data, Api, Jobs, Web
        security/                     Lamama.Security.*: Trivy
    focuma/
      Focuma.AppHost                  ports one band up, Keycloak on 8081, realm focuma
      Focuma.Names
      Focuma.Data                     the migrations of the spine's AppDbContext, PermissionSeed
      Focuma.Worker                   release runner
      admin/                          spec/primitives.tsp, Focuma.Admin.Primitives, .Api, .Web.Bff, .Web, .Shared

  tests/
    shared/                                  the foundations' tests, and what every suite builds on
      SpatialFocus.Testing                   fixtures and base classes, the host handed in: Postgres, Redis, test
                                             authentication, Keycloak admin, the API and page test bases, user
                                             management's arrangements, UiCulture
      SpatialFocus.Testing.Host              the shared test host: the spine, Api.Hosting, user management and the
                                             shared features, no product, schema by EnsureCreated
      SpatialFocus.Testing.Architecture      the convention rules, as a library
      SpatialFocus.Foundation.Tests          the foundations' unit tests: Web.Foundation, Web.Hosting,
                                             Localization, Primitives
      SpatialFocus.Foundation.IntegrationTests  the spine and the hosting libraries on the shared test host:
                                             auditing, read-only contexts, event streams, authentication
    lamama/
      Lamama.ArchitectureTests        the rules applied to Lamama and the shared projects
      Lamama.Admin.Web.UnitTests      the host's own: its localization, later its slices' view models and forms
      Lamama.IntegrationTests         Lamama's slices and its composition: endpoints, documents, permission seed
      Lamama.E2E                      boots Lamama.AppHost: Aspire system, UI, status page, and the shared
                                      features' E2E classes through one-line subclasses
    focuma/
      Focuma.ArchitectureTests        the rules applied to Focuma and the shared projects
      Focuma.E2E                      boots Focuma.AppHost: login, the releases' E2E classes through subclasses,
                                      its own deploy in simulation

  tools/
    SpatialFocus.ReleaseCli                  the PoC CLI, for runs outside the dashboard
```

- **Solutions**: work happens in `lamama.slnx` or `focuma.slnx`, each closed over its own projects and the shared ones.
  A change to shared code shows its effect on the other product when that solution builds, at the latest in CI.
- **Products** are composition plus their own slices: the AppHost, the `Program.cs` lines, the menu, the
  configuration, the realm. `Admin.Shared` holds the product's culture list from the start, which the API, the BFF and
  the client pass to the hosting libraries. As long as a product has no API slices of its own, its admin spec and
  `Admin.Client` stay out.
- **Layers** (decisions 12 to 14):

  | Project | References |
  |---|---|
  | Foundations, `Data`, `Localization`, `Names`, `Web.Bff.Shared`, the generated libraries | each other |
  | Feature | the foundations (`Primitives`, `Http`, `Data.Foundation`, `Server.Foundation`, `Api.Foundation`, `Web.Foundation`, `Web.ClientServiceDefaults`) and its own projects |
  | User management | the foundations and `Data` |
  | Hosting | the foundations, `Localization`, `Names`, `Web.Bff.Shared`, no feature and no vertical. `Web.Bff.Hosting` takes the user's permissions from the product's `IUserPermissionSource`. |
  | Product | everything it composes, the other product excepted |

  `SpatialFocus.Testing.Architecture` holds the rules, and each product's architecture tests apply them.
- **Two BFF libraries and a contract**: the BFF runs on ASP.NET Core and the client on WebAssembly, which has no runtime
  pack for `Microsoft.AspNetCore.App`, so `Web.Bff.Hosting` and `Web.Hosting` stay apart. What they share is
  the contract in `Web.Bff.Shared`, the BFF's part of today's `Admin.Shared` (`UserInfoResponse`, `UserClaim`,
  `BffClaimTypes`, `AnalyticsConfiguration`). Each product keeps a thin BFF project of its own, since the BFF serves
  its client through the project reference `MapStaticAssets` builds its manifest from. It also registers the
  `IUserPermissionSource` `/bff/user` asks: `UserPermissionSource` over user management's `UsersClient`, added with
  `AddApiClient<UsersClient>()`. The template's request diagnostics (`/debug/request` and its page) stay the product's.
- **The BFF's settings from the product**: the template hard-codes the session cookie (`__Host-focus.session`) and the
  API's address (`https+http://admin-api/`). Cookies on `localhost` do not depend on the port (RFC 6265, 8.5), so the
  two products' BFFs side by side (**Ports**) would overwrite each other's session. `Web.Bff.Hosting` takes both
  from the product's configuration.
- **The rest of today's `Admin.Shared`**: the localization machinery, which `Admin.Api` and `Admin.Web` both use, goes
  into `SpatialFocus.Localization`, a plain `net11.0` library the server and the WebAssembly client can both reference.
  It sits beside `Data` without the vertical's name, since nothing in it is Admin's: any API and client on ResXGenerator
  and FluentValidation can use it, a public side included. The product's `Admin.Shared` keeps `Cultures` and the
  contracts of its own slices.
- **A product's Admin API**, as an example of what stays:

  ```csharp
  WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
  builder.AddServiceDefaults();
  builder.AddApiDefaults();
  builder.AddSignalBackplane(ConnectionNames.Redis);
  builder.AddApiAuthentication();
  builder.AddApiLocalization(Cultures.Supported);
  builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

  builder.AddUserManagement(ConnectionNames.Database, ConnectionNames.ReadOnlyDatabase, AdminPermissions.All);
  builder.AddReleases(ConnectionNames.Database, ConnectionNames.ReadOnlyDatabase);

  WebApplication app = builder.Build();
  // the Use… lines in their order, MapApiReference, MapUserManagementEndpoints(), MapReleasesEndpoints()
  ```

  `AddMediator()` stays, since its source generator writes it into the host's assembly, and so do the `Use…` lines:
  provisioning sits between authentication and authorization, and eight visible lines keep that order checkable.
  `AddApiDefaults()` holds what every API sets up (`TimeProvider`, ProblemDetails, the JSON setting), so a public API
  calls it too. Redis with the backplane is a line of its own for a host with event streams, and the spine's contexts
  come with `AddUserManagement()`, since user management alone works on `AppDbContext`. The BFF's `Program.cs` is about
  five lines, the client's the foundation and hosting registrations plus each feature's `Add…Pages()`.
- **Databases**: one per product. Each holds the spine's identity schema and one schema per feature the product
  includes.
- **Ports**: both AppHosts and both E2E suites can run side by side. Lamama keeps the template's ports (Keycloak 8080,
  BFF 5770/7770, API 5201, persistent Postgres 15432), Focuma moves one band up (8081, 5870/7870, 5301, 15433). Each
  product has its own realm file, and in production its own realm or client on the shared Keycloak.
- **CI**: a change under `src/shared/` or `tests/shared/` builds and tests both products, one under `src/<product>/`
  or `tests/<product>/` only that product. Images go to `focus.azurecr.io`, deploys run through Flux as today.
- **Deployment** stays independent: each product builds its own images from the shared code, migrates its own
  database and is released on its own schedule. A shared change that one product is not ready for stays backward
  compatible or behind a setting, or that product builds from its last tag.

## Worker

Releases, monitoring and Trivy run code without a request: the release runner, health checks every few seconds,
scheduled scans, cleanup. Each product has a Worker host for them, a project without HTTP, deployed as its own
container with one replica:

- monitoring keeps feeding the status page while the Admin API restarts or is down
- Trivy's CPU, disk and binary stay out of the API image
- jobs run once, however many API replicas run
- a release of `Admin.Api`, `Web` or `Bff` leaves the runner alone. When the Worker image is part of a release, it goes
  last, after everything else is verified.

A feature has two entry points: `AddReleases()` in `Releases.Api` for the API and `AddReleasesJobs()` in `Releases.Jobs`
for the Worker. The split keeps the web stack out of the Worker.

**Communication** goes through the database, with Redis as the doorbell:

```
Browser ── POST /releases/{id}:confirm ──► Admin.Api ── writes the intent ──► Postgres
                                              │ publishes ReleaseRequested (Redis)
                                              ▼
                                           Worker ◄── also polls every few seconds ── Postgres
                                              │ claims the work, runs the step, writes the result
                                              │ publishes ReleaseChanged (Redis)
                                              ▼
Browser ◄── SSE /releases:watch ── Admin.Api (event streams) ◄── Redis
        └── GET /releases/{id}: reads the new state through the API
```

1. The database holds every state. The API writes intent as feature data in the same transaction as the business
   change: a release in "requested", a step confirmed.
2. Redis carries thin signals, the id and what kind of change. Pub/sub drops a signal nobody listens to, so the Worker
   also polls, and a missed signal costs seconds.
3. Work is claimed so it runs once: a Postgres advisory lock per environment for the runner, `FOR UPDATE SKIP LOCKED`
   for queue-like work such as scans.
4. Progress goes back the way `GroupChanged` does today: the Worker writes the state and publishes over the
   backplane, the Admin API's event streams push it to the browsers, and the page reads the state through the API.
5. Steps are idempotent and resume after a restart.
6. The API never calls the Worker. The overview's four truths (Git, Flux, cluster, host) are a snapshot the Worker
   refreshes into the database, which also keeps GitHub's and Kubernetes' rate limits in one place.

Still to do for it, with the first job:

- **`SpatialFocus.Server.Foundation`**, web-free, for what a feature's jobs build on (decision 13): the backplane's
  publishing half and the job-loop base. `SignalBackplane` in `Api.Foundation` splits. `Publish<T>` and
  `CloseStreamsFor` only write to Redis and move. The receiving half stays with the event streams in `Api.Foundation`:
  `BackplaneSubscription<T>`, the listener for close requests, closing everything after a reconnect. `Api.Foundation`
  references the new project.
- **The signals' JSON**: today publisher and subscription take the type info from ASP.NET Core's `JsonOptions`, whose
  resolver chain the generated `Add…Endpoints()` fills, and a Worker has neither. Each signal brings its generated
  context when registered, `services.AddSignal<GroupChanged>(UserManagementJsonContext.Default)`, and the backplane
  builds the type info from options on `JsonSerializerDefaults.Web` with that context as resolver. The payload stays
  camelCase and case-insensitive as today. The context's own `Default.GroupChanged` would write PascalCase and read
  case-sensitively, so during a rolling update (`maxUnavailable: 0`, decision 10) the new processes would drop the old
  ones' signals.
- **`SpatialFocus.Worker.Hosting`** keeps what the host alone runs: a heartbeat row, so the overview shows "runner
  offline", the registration of the job loops, and the Worker's health check for Aspire.

HTTP or gRPC between API and Worker, a message broker and a job framework stay out. `PeriodicTimer` loops cover
"every n seconds" and "nightly"; Quartz.NET with its Postgres store is the first candidate once scheduling grows, mind
its reflection against the AOT rule.

## Tests

- Shared code is tested once. A package keeps its tests beside its projects (`src/shared/admin/user-management/`,
  `src/shared/features/<name>/`), the foundations' tests and the test libraries sit under `tests/shared/`. Each product
  tests its slices, its product features and its composition, and each product's E2E suite boots its own AppHost.
- A package defines, the host executes, wherever the package cannot run alone, as a feature ships its migrations and
  the AppHost applies them. A feature's E2E tests are abstract classes on `BffPageTest` in its `.E2E` library (on
  `xunit.v3.extensibility.core`, no runner), which each product's suite runs through one-line subclasses in its own
  collection, on its own AppHost and session.
- The convention rules live in `SpatialFocus.Testing.Architecture`, and each product's architecture tests apply them to its
  own assemblies and the shared ones, so no solution pulls in the other product. `FeatureTests` allows a feature the
  foundations alone, and `ProjectReferenceTests` keeps the shared projects off the products and the general ones off
  the verticals (decision 14). A test project in a package folder may also reference the test libraries, while the
  package's production projects stay held to the foundations.
- The shared integration tests (`SpatialFocus.Foundation.IntegrationTests` and each package's `.IntegrationTests`) run
  on `SpatialFocus.Testing.Host`: the spine, `Api.Hosting`, user management and the shared features, no product.
  `Api.Hosting` makes that host a few lines. The spine's migrations are a product's (decision 15), so that host creates
  the spine's tables with `EnsureCreated`, adds the permission rows of its `TestHostPermissions` by hand, and each
  package's tables with `CreateTables`. The foundation tests use the spine's tables and user management's routes alone,
  so they hold in both repositories.
- The fixtures live in `SpatialFocus.Testing` (Postgres, Redis, `KeycloakAdmin`, `TestAuthenticationHandler`, the API
  and page test bases, generic over the host's `Program`, connection names, permissions and AppHost), so the shared
  tests and both products' suites start from the same ones. Each product derives its `ApiFixture`, `AdminAppFixture`
  and `AdminIngressAppFixture`, and keeps the xUnit collection definitions, which xUnit finds only in the running
  assembly.

## Porting

- **Template to repository**: `git format-patch` from the template, the rename map for host code
  (`FocusTemplate.Admin.*` → `Lamama.Admin.*`, `FocusTemplate.AppHost` → `Lamama.AppHost`, …), `git am -3`. Spine,
  hosting libraries, user management and shared features carry `SpatialFocus.*` in both repositories and need no
  renaming. The path map goes by name prefix, since the template's `src/admin/` holds user management and hosts side by
  side: a `SpatialFocus.*` project goes to `src/shared/` with its subfolder (`src/SpatialFocus.Web.Foundation` →
  `src/shared/SpatialFocus.Web.Foundation`, `src/admin/user-management/` → `src/shared/admin/user-management/`), a
  `FocusTemplate.*` project to `src/<product>/` under the rename map. A template change to host code is applied a
  second time with `Focuma.*`, which the hosting libraries keep rare: sign-in, BFF, client plumbing and AppHost
  extensions are shared code, and the host projects hold composition.
- **Repository to template**: the same maps in reverse, for what is generic. Candidates: the Worker host, the SSR
  public web, the inventory and reference patterns, the test host for features.
- Ports stay cheap while the shared folder keeps the template's layout and file names, and product code lives in
  `src/<product>/`.

## Step 0: in the template, before the fork

**The spine as `SpatialFocus.*`** (decision 2): `FocusTemplate.Primitives`, `.Http`, `.ServiceDefaults`,
`.Data.Foundation`, `.Api.Foundation`, `.Data`, the spec project, user management, the hosting libraries and the
features (`SpatialFocus.Weather.*`) take the shared prefix. `FocusTemplate.Admin.Web.Foundation` and
`.Admin.Web.ClientServiceDefaults` lose the vertical as well and move from `src/admin/` to `src/`, as
`SpatialFocus.Web.Foundation` and `SpatialFocus.Web.ClientServiceDefaults`. `Admin.Web.UnitTests` splits, since its
one reference is the host and it tests host code: the tests of the shared projects (Foundation, user management,
weather, `PermissionView`) go to `SpatialFocus.Web.UnitTests`, which references those projects directly, and the
host's own (the demo form, the host's localization) stay in `FocusTemplate.Admin.Web.UnitTests`.
The template's hosts keep `FocusTemplate.*`, `FocusTemplate.Names` among them, since each product names its own
resources. The `library-namespace` in every `tspconfig.yaml` becomes `SpatialFocus.Http`.

**User management into the Admin spine** (decision 12), under `src/admin/user-management/`:

- The spec moves from `src/admin/spec/user-management/` into its own folder as a service of its own
  (`SpatialFocus.Admin.UserManagement`). The spine's `primitives.tsp` imports its `permissions.tsp`, and the Admin
  `api.tsp` loses the two imports. The permission names (`UserManagement.ViewUsers`) stay, so the database and the
  seed stay as they are. The OpenAPI schema names lose their `UserManagement.` prefix, since the models now sit in
  their service's own namespace, as weather's `StationRenameRequest` does.
- `Shared` (generated, plus `GroupRequest` and `UserSearchRequest` resx from `Admin.Shared`), `Client`, `Api`, `Web`
  projects with `AddUserManagement(database, readOnlyDatabase)`, `MapUserManagementEndpoints()` and
  `AddUserManagementPages()` with its `PageAssembly`. No `Data` project: the tables stay the spine's identity in `Data`,
  and `AddUserManagement()` registers its two contexts, which leave `Admin.Api/Program.cs`. Both registrations take the
  product's permission list and register it as `PermissionCatalog`.
- Along with the handlers and pages come the pieces every Admin host needs: `/users/me` (same route), the provisioning
  middleware, the permission handler, `UserPermissions`, the administrator bootstrap, `PermissionLabels`, and
  `AddSignal<GroupChanged>()`, which `AddUserManagement()` registers. The product's BFF reaches `/users/me` through
  `Admin.UserManagement.Client` and hands it to `Web.Bff.Hosting` as `IUserPermissionSource`.
- The integration tests stay in `Admin.Api.IntegrationTests` with new usings. The OpenAPI tests expect
  `user-management.yaml` beside `admin.yaml` and `weather.yaml`, which Scalar picks up by itself.

**The hosting libraries** (decision 13): `SpatialFocus.Api.Hosting`, `.Web.Bff.Hosting`, `.Web.Bff.Shared`,
`.Web.Hosting` and `.AppHost.Hosting` under `src/`, with the contents the structure above lists, and the BFF's cookie
name and API address as settings. `Admin.Shared` gives up its generic parts: the BFF's types to `Web.Bff.Shared`, the
localization machinery to `SpatialFocus.Localization`. The template's hosts keep their composition and the demo slice.
The Public API calls `AddApiDefaults()` for the lines both APIs share (`TimeProvider`, ProblemDetails), which keeps the
libraries honest about assuming no vertical. `Server.Foundation` and `Worker.Hosting` wait for the first job.

**`FeatureTests` as an allowlist** (decision 14), so `Data` and user management stay closed to weather without a
named exception.

**The form styles into Foundation**: about 130 lines of the host's `app.css` (`.app-field`, `.app-message*`, the
validation states) style Foundation's `Forms/` components. They move into a Foundation stylesheet the host's
`index.html` loads, so a feature's form looks the same in every host.

**Beyond the first plan**, found while doing it:

- The request diagnostics (`/debug/request`, `RequestDiagnostics`, the page) stay the template's: its BFF maps the
  endpoint in Development, `Admin.Shared` holds the record, and `Admin.Web` the page. `NotFound` stays the product's
  too, since it names the product's layout.
- Typed ids and permissions per package (decision 11): `SpatialFocus.Weather.Primitives` beside the feature,
  `FocusTemplate.Admin.Primitives` with `AdminPermissions` from `src/admin/spec/primitives.tsp`, after the emitter's
  `primitives-namespace` and `permission-list`. `gen.mjs` compiles a `*.Primitives` project from the
  `spec/primitives.tsp` beside it.
- The product owns the spine's migrations (decision 15): `FocusTemplate.Data` with `PermissionSeed`, the design-time
  factory in `FocusTemplate.Admin.Api`.
- `Web.Bff.Hosting` asks `IUserPermissionSource`, and `ProjectReferenceTests` checks the reference rules (decision 14).
- `SpatialFocus.Names` holds the design-time names, so the AppHost references no data project and carries no EF Core:
  `AddMigrations(…)` takes the context's full name as a string, which `MigrationResourceTests` checks.
- The solution groups the shared projects in a `/shared/` folder.

Done in the template, one commit each: user management, the hosting libraries one by one, the allowlist, the form
styles, the unit-test split, the rename to `SpatialFocus.*`, the docs, the emitter bump, and the restructure above in
one commit, since its parts share docs and lock files.

## Step 1: fork, strip and split

Goal: a green build, green tests and a working `aspire start` of Lamama with the shell and user management.

- **Public vertical**: `src/public/` (Api, Client, Shared, Mobile, Mobile.ServiceDefaults, the spec with its
  `Public.Spec` project), `tests/FocusTemplate.Public.Mobile.E2E`, the MAUI device resources and the Dev Tunnel in the
  AppHost, `Features:Mobile`, `ResourceNames.PublicApi`, the MAUI, DevTunnels and Maui.Core entries in
  `Directory.Packages.props`. The build no longer needs the MAUI workloads.
- **Validation demo**: `src/admin/spec/demo-profiles/`, `Features/DemoProfiles/` in Api and Web, `DemoProfileCustomRules`
  in `Admin.Shared`, the six `Validation*Tests` of the E2E suite and the demo's unit tests. `Foundation/Forms/` and its
  unit tests (which use their own test form) stay.
- **Weather**, a feature package, leaves as one: the folder `src/features/weather/` (spec with its typed ids and
  permissions, `Primitives`, `Shared`, `Client`, `Data` with its migrations and seed, `Api`, `Web`) and its solution
  folder, then the host's lines for it: the import in `src/admin/spec/primitives.tsp`, `AddWeather` and
  `MapWeatherEndpoints` in `Admin.Api`, `AddWeatherPages` in `Admin.Web`, `AddMigrations("weather-migrations", …)` and
  the waits on it in the AppHost, the menu entry in `MainLayout`, the Public API's forecasts (which leave with the
  Public vertical), `MigrationResourceTests`' weather context, and the project references. PostGIS was the feature's
  requirement: back to the plain Postgres image (AppHost and Testcontainers).
- **Tests on weather**: `WeatherTests`, `StationsTests` (E2E and integration), `WeatherForecastTests`,
  `StationTextsTests` and `WeatherPagesTests` go. Tests that use weather only as their example endpoint switch to the
  groups or users: the authentication and ingress tests, the OpenAPI document and API reference tests (which then
  expect `user-management.yaml` alone), the auditing and read-only context tests, `TelemetryTests` (any page with an API
  call).
- **The Admin spec leaves**: after step 0 it holds the demo profiles alone, so with them `src/admin/spec/`,
  `Admin.Client`, the generated part of `Admin.Shared` and `AddAdminEndpoints()` with `MapAdminEndpoints()` go.
  `Lamama.Admin.Shared` keeps `Cultures`. `MapApiReference` takes `user-management` as its default document, and
  `releases` once Releases is in.
- **Migrations**: the spine's, in `Lamama.Data`, squashed into one fresh `InitialCreate`. The weather schema's history
  leaves with the feature, and `AdminPermissions` loses `Weather.ManageStations` with the import, so the fresh seed has
  user management's permissions alone.
- **Docs and rules**: the Public vertical (`docs/architecture.md`, the `mobile` doc and rule), `docs/web-forms.md`
  (rewritten around the Groups form; the async server rule and list rows lose their example until a Releases form needs
  them), and every mention of the removed tests. Weather is the docs' only feature example (architecture, database,
  spec-first, web pages, the feature-slice skill): the text stays and the example becomes Releases once it exists.
- Check `PersistentDatabaseTests` and the AppHost seed path. The weather seed left with the feature's `AddWeather()`.

Then the split into the structure above: the spine, `Web.Foundation`, `Web.ClientServiceDefaults`, `Localization` and
the hosting libraries to `src/shared/`, user management to `src/shared/admin/user-management/`, the fixtures into
`SpatialFocus.Testing`, the hosts to `src/lamama/` as `Lamama.*` with the culture list in `Lamama.Admin.Shared`
(`FocusTemplate.Data` becomes `Lamama.Data`, `FocusTemplate.Admin.Primitives` `Lamama.Admin.Primitives` with
`src/lamama/admin/spec/primitives.tsp`), the host's unit tests to `tests/lamama/Lamama.Admin.Web.UnitTests`,
`lamama.slnx`, the rules' path globs, `gen.mjs` for the spec locations, the porting script. Then the emitter's
`tspconfig.yaml` namespaces, which follow the project names.

## Step 2: shell and design (Lamama)

- **Radzen 12**: the template runs 12.1.2 since 2026-10-13, the click dummy 12.0.1. 12.0's breaking changes (DatePicker
  drill-down by default, chart axes, saved DataGrid settings, dynamic LINQ marked for trimming) touched nothing in the
  template. The click dummy's pages get checked against them when they move in, its date pickers and charts first.
- **Design system**: DESIGN.md, `.impeccable/` and the click dummy's `app.css` (about 2,000 lines: Radzen's software
  theme recoloured to the Landmanager green, the "cadastral sheet") move in beside the template's form styles, split by
  owner: tokens, the Radzen theme variables and the shell go to the host; component styles go to `.razor.css` files in
  `Releases.Web`; state semantics (sync, drift, maintenance, …) are custom properties in the feature's stylesheet with
  fallbacks, so the host's palette wins where there is one. The template's dark-theme toggle goes unless the design
  gets a dark variant.
- **Shell**: "Lamama · Releases" header with the module switcher of the click dummy (Releases, Benutzerverwaltung, the
  planned modules greyed out: Mitteilungen, Monitoring, Kunden, Datenschutz, Protokoll), on top of the template's
  `MainLayout` with login, logout and the permission-driven menu.
- **German texts** (decision 5), including `PermissionLabel` and the user-management pages.

## Step 3: the release library moves in

```
src/shared/features/releases/
  spec/                                 SpatialFocus.Releases.Spec: api.tsp, primitives.tsp, permissions.tsp, tspconfig.yaml
  SpatialFocus.Releases.Primitives/            ReleaseSetId, ReleaseId, ReleasesPermissions (generated)
  SpatialFocus.Releases.Shared/                wire contract (generated)
  SpatialFocus.Releases.Client/                typed clients (generated)
  SpatialFocus.Releases.Data/                  entities, ReleasesDbContext and its read-only twin, schema releases
  SpatialFocus.Releases.Api/                   handlers, AddReleases(), MapReleasesEndpoints() (generated)
  SpatialFocus.Releases.Jobs/                  the runner, AddReleasesJobs() for the Workers, on Server.Foundation
  SpatialFocus.Releases.Web/                   pages and components, AddReleasesPages()
  SpatialFocus.ReleaseManagement/              the PoC library, no knowledge of either product
  SpatialFocus.ReleaseManagement.Simulation/   the simulated world, see below
  SpatialFocus.ReleaseManagement.Tests/        the library's and the simulation's tests
  SpatialFocus.Releases.Tests/  .IntegrationTests/  .E2E/   the feature's tests, see Tests
tools/
  SpatialFocus.ReleaseCli/                     the PoC CLI, for runs outside the dashboard
```

- The library stays a separate assembly beside the feature, referenced by `Releases.Jobs`, where the runner lives,
  and in simulation through `ReleaseManagement.Simulation` by `Releases.Api`, which serves the simulated hosts (step 4).
  Beyond that the API works on the database. The library keeps the PoC rule: it
  knows nothing about Lamama, Focuma, EF Core, Mediator or configuration; paths, marker keys, hosts and commit messages
  come from the caller. An architecture test checks its references, beside `FeatureTests`, which holds the feature to the
  foundations.
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
| Simulation (default) | `aspire start`, CI, E2E, demos | the simulated world, in the product's database (schema `simulation`) | nothing |
| Sandbox (opt-in in `appsettings.local.json`) | a developer working on the integration | the Infrastructure repository, dev2/test2/staging2 | kubeconfig context `satgrass-dev`, GitHub token, Scaleway key in user secrets |
| Production | the deployed products only | LamaInfra (test, staging, prod) for Lamama, the Focus projects' GitOps repository for Focuma | GitHub App, read-only ServiceAccount, Scaleway IAM application |

The sandbox is shared: every developer's instance has its own database, so "one release per environment" holds only
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
resume after a restart. Focuma's E2E suite uses it to release Focuma itself, the Worker last.

**Where the world lives.** Two processes need it: the runner in the Worker commits into it, and the API serves the
simulated hosts from it (**URLs**). So it lives in Postgres, in schema `simulation` of the product's database, with a
context and migration of `ReleaseManagement.Simulation` that the AppHost applies when the flag is on. Since everything
else follows from Git and the clock, the stored state is small: the commits with their files and timestamps, and the
failure switches. The registry's tags stay configuration.

**URLs.** Public host, release host and version path per environment become configuration (the click dummy builds
`https://app.{Name}.landmanager.de/` in code). In simulation they point at simulated hosts the API serves from the stored
world, so a demo never links to real Landmanager and the manual-check link opens something:

- `/simulation/{env}/app/`: a version page, or the maintenance page with 503 and `Retry-After` while maintenance is
  applied
- `/simulation/{env}/release-host/`: the new version while the release-host marker is on, 404 otherwise
- `/version.json` under both

The probe in simulation is then the real `HttpHostProbe`, which gets tested on the way. Locally the API's port is
reachable; a deployed demo needs a BFF route for `/simulation/*`.

**Limits.** The simulation never runs the real GitHub, Kubernetes and Scaleway adapters; those stay with the sandbox and
with HTTP-level tests per adapter if they get any. World and releases share the database, so a Worker restart keeps
them together, and with `Features:PersistentDatabase` on both survive an AppHost restart.

Unit tests of single steps keep the PoC's scripted fakes.

## Step 5: the Releases feature

**Spec**: `src/shared/features/releases/spec/api.tsp`, a `@service` of its own in namespace `SpatialFocus.Releases` with
`@useAuth`, one file per interface (environments and the overview, release sets, releases), so the code lands in
`Releases.Api/<Interface>/`. The click dummy's `Models/` are the draft of the contract. Actions are custom methods:
`POST /releases/{id}:confirm`, `:reject`, `:revert`, `POST /release-sets/{id}:promote`. The typed ids (`ReleaseSetId`,
`ReleaseId`, …) go into the feature's `primitives.tsp` in namespace `SpatialFocus.Releases.Primitives`, which
`SpatialFocus.Releases.Primitives` generates with `primitives-namespace`. It imports the spine's `primitives.tsp` and
its `permissions.tsp` (namespace `SpatialFocus.Releases.Primitives.Permissions`, decision 3), for example `ViewReleases`,
`PlanSets`, `ReleaseTest`, `ReleaseStaging`, `ReleaseProd`. The Api project sets `omit-unreachable-types: true`, as
weather's does. Each product's `admin/spec/primitives.tsp` imports the feature's with one line, which puts its
permissions into the product's `AdminPermissions`.

**Data**: `ReleaseSet`, `Release`, `ReleaseStep` as plain entities with auditing and a row version on the set, in
`Releases.Data` with schema `releases`, `ReleasesDbContext` and its read-only twin, and its own migrations. Who started
or confirmed a release points at the spine's users through `UserReference`. Products, environments and components come
from configuration first, as in the CLI; the architecture page models them as entities, which can follow.

**Rules** in the handlers, answered with 409 cases: one release per environment, the proven chain
(`requiresProvenOn`), the hotfix rule (`x.y.z.n` only where `x.y.z` runs), prod confirmation. The click dummy's
`DemoStore` (`Blocker`, `ProvenOn`, `EligibleSets`) is their specification.

**Runner**: a job in the product's Worker, registered by `AddReleasesJobs()` from `Releases.Jobs` on
`Server.Foundation`'s job-loop base, that runs one step at a time, persists
each step's record and the `DesiredRevision` after commit steps, and resumes after a restart. One runner per
environment at a time, through a Postgres advisory lock. The product conventions (file paths, markers, commit
messages) move from the CLI's `LamamaRelease.cs` into the feature, per product (decision 6). Every step commits once,
with a message naming release, component and version, so a manual revert is `git revert` of that commit.

**Live status**: the template's server-sent events. A `watch(): SSEStream<ReleaseEvents>` on `/releases:watch`, the
Worker publishing a `ReleaseChanged` through `Server.Foundation`'s publisher after each step, and the pages listening on a
`LiveUpdates<ReleaseChanged>`, as the groups list does. The stream runs through `/_api` like any call, since the
client reads it with `HttpClient` and sends the `X-CSRF` header.

**Host composition** in each product, one line each: `builder.AddReleases(ConnectionNames.Database,
ConnectionNames.ReadOnlyDatabase)` and `endpoints.MapReleasesEndpoints()` in `Admin.Api`, `AddReleasesJobs(…)` in the
Worker, `AddReleasesPages(environment, ResourceNames.AdminApi)` in `Admin.Web` (which registers its `PageAssembly`),
`AddMigrations("releases-migrations", "SpatialFocus.Releases.Data.ReleasesDbContext", …).WaitForCompletion(migrations)`
in the AppHost, the menu entry in `MainLayout`, and the import in the product's `admin/spec/primitives.tsp` with a
migration in its `Data` for the new permission rows and the administrators' grants.

**UI**: the click dummy's pages and components ported to `ViewModelPage` and the generated clients in `Releases.Web`,
markup kept, CSS split as in step 2.

**Stages**, as the architecture page orders them:

1. Overview, read-only: the four truths per component and environment, drift, maintenance state, failures per cell.
2. Maintenance switch for staging and prod, as a release of its own (this brings the runner).
3. Release sets: plan on test, promotion with the proven chain, history, revert.
4. Release host, smoke tests, optional manual check.
5. Hotfix rules, Bitbucket changelog, release notes.

UptimeRobot, Teams and Bitbucket come with their stages, each behind an interface with a recording fake in simulation.

## Step 6: Focuma

The second host on the shared folder: `Focuma.AppHost` one port band up with its own realm, `Focuma.Names`,
`Focuma.Admin.Api`, `.Web`, `.Web.Bff` and `Focuma.Worker`, composing user management and Releases. Its products: the
Focus projects, Lamama and Focuma itself. Its architecture tests and E2E suite, the readiness probes and
`maxUnavailable: 0` on its Deployments, and the self-deploy runbook in `docs/focuma/` (decision 10).

## Step 7: Lamama's monitoring, security and status page

Product features under `src/lamama/features/`, each with its jobs in Lamama's Worker:

- **Monitoring**: checks per component and environment (health endpoints, `/version.json`, certificates), results with
  retention, incidents. It brings the "UptimeRobot" stage of Releases closer and feeds the status page.
- **Security**: Trivy scans of the images the inventory knows, findings per image and version, a view per component.
- **Status page**: `Lamama.Public.Web`, server-rendered, reading monitoring's read-only context.

Monitoring and security work on the same products, environments and components as Releases. When the three need one
list, it moves out of Releases' configuration into the spine, configuration first, entities later, with a reference
shape like `UserReference`.

## Outside the code

Needed before the products release anything real:

- GitHub App with `Contents: write` on LamaInfra, on the Focus projects' GitOps repository, and on Infrastructure for
  the sandbox
- Scaleway IAM application with `ContainerRegistryReadOnly`, read access to `focus.azurecr.io` for Focuma
- read-only ServiceAccount with its ClusterRole on dev and prod
- Keycloak clients for Lamama and Focuma
- Teams webhook
- LamaInfra caught up with the sandbox (open on the architecture page): wildcard certificate and DNS per environment,
  maintenance page with 503, the `witty-dune` ingress with its marker, relative redirect URIs in the frontend, the
  Keycloak redirect URI and web origin for the release host
- readiness probes and `maxUnavailable: 0` on the Deployments of every product a runner releases
- where Lamama and Focuma themselves are deployed, following the template's Flux pipeline

## Open decisions

1. The repository's name.
2. Step 0 in the template before the fork (recommended): user management, the hosting libraries and the rename. Or all
   of it in the repository and ported back.
3. Whether monitoring and security get a `Lamama.*` start or go to the shared folder at once, should Focuma want them
   early.
4. The product's culture list in `<Product>.Admin.Shared` (proposed) or in configuration.
5. Permission labels of a feature: `PermissionLabels.resx` knows user management alone, so `Releases.ReleaseProd`
   reads off its name in English inside the German UI. Proposed: each feature brings its own labels resx through a
   hook in `Web.Foundation`.
6. `UserManagementSeed` in `Data` seeds the made-up users and groups. It works there, and its name points at user
   management: rename it to `IdentitySeed`, or let user management seed through a hook as `AddWeather()` does.

## Risks

- **Template drift**: the template keeps changing; each port gets harder the longer it waits. Host changes are ported
  twice, which the hosting libraries keep to composition.
- **Shared changes**: both products build from the current shared code, so a change one product is not ready for
  needs care (decision 1, **Structure**).
- **Permissions of the other product** in each groups page until the emitter change (decision 11).
- **Self-deploy**: a Focuma version that starts and misbehaves needs a manual revert (decision 10).
- **Forms without the demo**: the validation machinery (async rules, debounce, list rows) loses its only consumer until
  a Releases form needs it.
- **The docs' feature example**: weather leaves, and Releases takes its place only once it exists.
- **Shared sandbox**: two developers releasing to test2 at once collide.
- **Reflection-based clients** in the server, against the AOT rule.

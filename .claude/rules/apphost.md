---
paths:
  - "src/*.AppHost/**"
  - "src/*.AppHost.Hosting/**"
  - "src/*.ServiceDefaults/**"
---
# AppHost and service defaults

- A feature flag is `Features:<Name>` in the AppHost's `appsettings.json` with its default, overridable in the
  gitignored `appsettings.local.json`.
- A flag that needs setup a plain `aspire start` lacks (Dev Tunnel, emulator), or that changes how the dev environment
  behaves (persistent containers, chaos), defaults to off.
- The E2E fixtures pin every flag as an argument, and a new flag gets pinned there too.
- Authentication has no flag.
- What every product's AppHost runs the same way goes into `FocusTemplate.AppHost.Hosting`, which takes the
  product's names as arguments. The AppHost keeps its composition, its settings and its realm file.
- A migrations resource is added with `AddMigrations<TContext>`, and an API waits for the completion of each one
  whose tables it reads.
- A resource or connection reference is named through `ResourceNames` or `ConnectionNames` in `FocusTemplate.Names`,
  and its consumers use the same constant. A feature takes names as arguments. The realm file names its client after
  the BFF's resource and its audience after the API's.
- A persistent container sets `IsProxied = true` on the endpoints the projects use, with a fixed port, so `localhost`
  also answers on `::1` (see **Persistent containers** in the doc).
- The preview integrations (`Aspire.Hosting.Blazor`, `Aspire.Hosting.Keycloak`, `Aspire.Hosting.Maui`,
  `Aspire.Hosting.EntityFrameworkCore`) move in lockstep with the AppHost SDK, and so does `Aspire.Hosting`, which
  `FocusTemplate.AppHost.Hosting` references.
- After an Aspire update, rerun `aspire agent init` for both skill locations, as **Updating Aspire** in the doc
  describes, and review what it changed.

Background: [docs/apphost.md](../../docs/apphost.md).
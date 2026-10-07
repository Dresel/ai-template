# AppHost and service defaults

What to follow when changing these files: [the rule](../.claude/rules/apphost.md).

## What the AppHost wires

Postgres with the database and its `migrations` resource (see [database](database.md)), both APIs, the BFF, Keycloak
(its own container or an existing realm, see [authentication](authentication.md)), and behind flags an nginx TLS
ingress, Umami analytics, and the MAUI device resources with a Dev Tunnel. `FocusTemplate.ServiceDefaults` gives every
server OTel, service discovery, health checks and the standard resilience handler.

Packages: `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.EntityFrameworkCore` (`AddEFMigrations`),
`Aspire.Hosting.Blazor` (the WASM client's service and telemetry proxying), `Aspire.Hosting.Keycloak`,
`Aspire.Hosting.Maui` with `Aspire.Hosting.DevTunnels`, and `CommunityToolkit.Aspire.Hosting.Umami`. Blazor, Keycloak,
Maui and EntityFrameworkCore are previews in lockstep with the AppHost SDK.

## Feature flags

In the AppHost's `appsettings.json`, overridable per developer in the gitignored `appsettings.local.json`. The E2E
fixtures pin all of them as arguments. Authentication is always on and has no flag.

| Flag | Default | On means |
|---|---|---|
| `Features:Analytics` | on | Umami and its Postgres, proxied through the BFF |
| `Features:TlsOffloadingIngress` | on | the nginx ingress on `https://localhost:7770` |
| `Features:Mobile` | off | the MAUI device resources and the Dev Tunnel, which need the devtunnel CLI and an emulator |
| `Features:LocalKeycloak` | on | a Keycloak container with the imported realm. Off takes an existing realm from parameters |
| `Features:PersistentLocalKeycloak` | off | that container and its data survive between starts |
| `Features:PersistentDatabase` | off | the dev Postgres and its data survive between starts |
| `Features:Chaos` | off | run mode only: the Admin API gets the AppHost's `Chaos` section, and `ChaosFilter` on the route group every slice maps onto delays and fails a share of the requests through Polly's chaos strategies, never the health probes |

## Updating Aspire

The agent skills come with the Aspire CLI: `aspire agent init` writes the skill text built into the installed CLI
version, so a CLI update can bring new or changed skills that only arrive when `agent init` runs again.

1. `aspire update --self` updates the CLI, `aspire update` the AppHost's Aspire packages. The preview integrations
   (`Aspire.Hosting.Blazor`, `Aspire.Hosting.Keycloak`, `Aspire.Hosting.Maui`, `Aspire.Hosting.EntityFrameworkCore`)
   move in lockstep with the AppHost SDK.
2. `aspire agent init --skill-locations standard,claudecode --skills all --non-interactive` refreshes the skills in
   `.agents/skills/` and `.claude/skills/`. Name both locations: the command removes the skills from every location it
   is not given.
3. Review the diff. The same run also:
   - upgrades the Playwright CLI, which the `playwright-cli` skill drives, as a global npm package (`@playwright/cli`),
     and that CLI may append its own entries to `.gitignore` (`.playwright-cli/`)
   - refreshes the user-level copies in `~/.agents/skills/`
   - installs the telemetry hooks described below
   - leaves `.mcp.json` alone

## Aspire telemetry

The Aspire CLI sends usage telemetry to Microsoft by default, the subject of its startup "telemetry notice".
`aspire agent init` adds to that: it installs hooks in the user-level settings of Claude Code (`~/.claude/settings.json`,
a `PostToolUse` hook) and of the Copilot CLI (`~/.copilot/hooks/aspire-telemetry.json`), both running a script in
`~/.aspire/hooks/`. The hook starts PowerShell after every tool call, in every project. When the call used an Aspire
skill, a file inside one, or an Aspire MCP tool, it sends an event through `aspire agent telemetry`: the event type,
the skill or tool name, the file's path inside the skill, the client and a timestamp, and per the script no absolute paths,
repository or user names. Every other call returns at once.

The only opt-out is the environment variable `ASPIRE_CLI_TELEMETRY_OPTOUT=true`, since the CLI has no setting for it. It
silences the CLI and the hook alike, but the hook still starts PowerShell after every tool call, so removing the hook
entries is a step of its own.

- **For a developer's machine**, as a user variable (`setx ASPIRE_CLI_TELEMETRY_OPTOUT true`). It covers every
  terminal, IDE and project, survives clones and reboots, and reaches processes started after setting it.
- **For Claude Code in this repository**, checked in through `.claude/settings.json`, so every session and everything it
  starts (the commands it runs, the `aspire agent mcp` server, the hook) inherits it:

  ```json
  "env": { "ASPIRE_CLI_TELEMETRY_OPTOUT": "true" }
  ```

  It does not reach `aspire` run from a terminal or an IDE, nor other projects.
# Splitting AGENTS.md into `.claude/rules/` — plan

*Written 2026-09-25, not started. The loading behavior below was checked that day against
https://code.claude.com/docs/en/memory. This file is the hand-over for the session that does the split.*

Every Claude Code session loads AGENTS.md (522 lines, through the `@AGENTS.md` import in CLAUDE.md) plus the Aspire
comments guide it imports (`.agents/comments-aspire.md`, 114 lines): about 52 KB of context before the first message,
most of it about areas the session never touches. The docs target under 200 lines per instruction file and name
path-scoped rules as the way past that. The split keeps a short core in AGENTS.md and moves the knowledge about each
area into a rule file that loads when Claude reads a file of that area.

## How rules load

- Every `.md` under `.claude/rules/` is picked up, subdirectories included.
- **Without `paths:`** a rule loads at session start, like CLAUDE.md: reorganized, but nothing saved. `@` imports load
  at session start too, so importing does not help either.
- **With `paths:`** a rule loads when Claude reads a file matching one of its globs, "not on every tool use". Do not
  count on a search over those files to load it. Edits are covered, since the Edit tool requires a Read first.
- `paths` is the only frontmatter field Claude Code reads: a YAML list or a comma-separated string of globs, brace
  expansion allowed (one rule's list may expand to at most 1,000 patterns). Frontmatter that does not parse makes the
  rule load at session start, as if it had no `paths`.
- Claude Code only. Codex, Copilot and Cursor have their own formats and do not read `.claude/rules/`; for them the
  index in AGENTS.md (below) is the pointer. Per-directory AGENTS.md files would be the cross-tool mechanism, but Claude
  Code ignores them here: by default it reads AGENTS.md itself only when there is no CLAUDE.md, and ours imports
  AGENTS.md instead.
- Procedures (add a migration, run the E2E suite, regenerate from the spec) fit skills better than rules: a skill loads
  when a task calls for it, a rule when a file is read.

## The dividing line

A path rule fires when a file is read. So:

- **Knowledge about files goes into a rule** with that area's paths.
- **Knowledge an action or a question needs stays in the core.** A path rule never fires before `dotnet test`, before
  a `sed` edit, or for an answer that opens no file. "Never run `dotnet format` on the MAUI project" has to be in
  context before the command, not after reading a MAUI file.

## Rule files

Each file moves text out of AGENTS.md; nothing stays in both places. Line counts are those of the current sections.

### `authentication.md`

From **Authentication** (79 lines) and the authentication packages under **Tech stack**.

```yaml
---
paths:
  - "src/admin/FocusTemplate.Admin.Web.Bff/**"
  - "src/admin/FocusTemplate.Admin.Api/Authentication/**"
  - "src/admin/FocusTemplate.Admin.Api/Features/**/*.Hooks.cs"
  - "src/admin/FocusTemplate.Admin.Web/{App.razor,_Imports.razor}"
  - "src/admin/FocusTemplate.Admin.Web/Infrastructure/Authentication/**"
  - "src/admin/FocusTemplate.Admin.Web/Foundation/Shell/MainLayout.razor"
  - "src/admin/FocusTemplate.Admin.Web.ClientServiceDefaults/{CsrfHeaderHandler,RedirectToLoginHandler,ServiceCollectionExtensions}.cs"
  - "src/admin/FocusTemplate.Admin.Shared/{BffClaimTypes,UserClaim,UserInfoResponse}.cs"
  - "src/FocusTemplate.AppHost/AuthenticationExtensions.cs"
  - "src/FocusTemplate.AppHost/keycloak/**"
  - "tests/FocusTemplate.Admin.Web.E2E/{AuthenticationTests,BlazorAppFixture,KeycloakAdmin}.cs"
  - "tests/FocusTemplate.Admin.Api.IntegrationTests/{AuthenticationTests,TestAuthenticationHandler}.cs"
---
```

### `database.md`

From **Database & migrations** (56 lines), the FocusTemplate.Data paragraph under **Projects** (entities, PostGIS
with longitude first, native enums, the two pools), the EF lines under **Tech stack**, **Read/write split**, and the
query half of **AOT-ready** (one method-syntax chain per query, converters without captured state).

```yaml
paths:
  - "src/FocusTemplate.Data/**"
  - "src/*/FocusTemplate.*.Api/Features/**/*Handler.cs"
  - "tests/FocusTemplate.ArchitectureTests/**"
```

### `integration-tests.md`

From **Integration test database** (21 lines) and the integration level under **Development loop**. The glob already
covers the planned Public integration test project.

```yaml
paths:
  - "tests/*.IntegrationTests/**"
```

### `spec-first.md`

From **Spec-first APIs** (45 lines), the TypeSpec, Mediator and Vogen lines under **Tech stack**, the Primitives and
emitter paragraphs under **Projects**, and **Shared DTOs**. A handler file loads both this rule and `database.md`,
which is where the two meet.

```yaml
paths:
  - "src/spec/**"
  - "src/*/spec/**"
  - "**/tspconfig.yaml"
  - "**/generated/**"
  - "**/openapi.yaml"
  - "src/FocusTemplate.Primitives/**"
  - "src/*/FocusTemplate.*.{Client,Shared}/**"
  - "src/*/FocusTemplate.*.Api/Features/**"
  - "package.json"
  - "scripts/gen.mjs"
```

### `mobile.md`

From the MAUI line under **Tech stack** (workload band, `VersionOverride`), **Mobile bugs/features** and the mobile
test level under **Development loop**, and **Mobile dev loop** and `AutomationId` under **Conventions**.

```yaml
paths:
  - "src/public/FocusTemplate.Public.Mobile*/**"
  - "tests/FocusTemplate.Public.Mobile.E2E/**"
```

### `web-e2e.md`

From the Aspire-system and UI test levels and **UI bugs** under **Development loop**, `data-testid` under
**Conventions**, and how the fixtures work: one AppHost per collection, one captured login, feature flags pinned as
arguments, the separate ingress fixture, `KeycloakAdmin`.

```yaml
paths:
  - "tests/FocusTemplate.Admin.Web.E2E/**"
  - "src/admin/FocusTemplate.Admin.Web/**/*.razor"
```

### `apphost.md`

From the AppHost paragraph under **Projects**, the orchestration and observability lines under **Tech stack**, and
**Feature flags** under **Conventions**.

```yaml
paths:
  - "src/FocusTemplate.AppHost/**"
  - "src/FocusTemplate.ServiceDefaults/**"
```

### `csharp.md`

From **Comments** (with `.agents/comments-aspire.md` moved in, keeping its source line), the rest of **AOT-ready**
(register by hand, no reflection-based discovery) and **Extension classes**. It loads in nearly every coding session;
what it saves is the sessions without code, such as planning or wiki work. Imports inside rule files are not
documented, so the guide's text moves into the rule instead of being imported.

```yaml
paths:
  - "**/*.cs"
  - "**/*.razor"
```

### `packages.md`

From the package lines under **Tech stack**: Central Package Management, lock files, the EF stack pinned to one
version, `Microsoft.Maui.Core` matching the workload band, and the stale-lock-file fix from the memory section below.

```yaml
paths:
  - "Directory.Packages.props"
  - "Directory.Build.props"
  - "global.json"
  - "**/*.csproj"
  - "**/packages.lock.json"
  - ".config/dotnet-tools.json"
```

### `claude-config.md`

From **Avoid permission prompts** under **Development loop**: how the allowlist matches (segment by segment, deny beats
allow), workspace trust, the double `C:\` / `C:/` registration on Windows, settings that load only at session start,
and the MCP servers.

```yaml
paths:
  - ".claude/**"
  - ".mcp.json"
```

## What stays in AGENTS.md

Target 150–180 lines:

- the intro and the project map, one line per project
- the essentials of the stack: .NET 11 preview, Central Package Management, analyzers as build errors
- **Running**, including "stop the AppHost before `dotnet build`/`dotnet test`"
- **Build, test, format**: `npm run gen`, build, test with the filter syntax, `dotnet format`, "never format the MAUI
  project", "no `sed -i` on source files"
- the **Agent toolbox** table, including "check the Aspire docs before recommending Aspire changes"
- **Development loop**: the seven steps, one line per test level pointing at its rule, runtime bugs through Aspire,
  scale-out bugs
- one line each for what spans areas: changing an API goes spec → `npm run gen` → handler → consumers; consumers use
  the generated clients and never hand-write HTTP (the Web project is outside `spec-first.md`'s paths); nothing under
  `generated/` is edited by hand; prefer allowlisted single commands
- the index: the rule files with one line on what each covers, the planning notes at the root (backlog.md and the
  others), and the Outline pages agents need (below)

## Promote from private agent memory

Some findings live only in one developer's Claude memory (`~/.claude/projects/<repo>/memory/`), so no colleague's agent
knows them. They belong in the repo:

- **Core, Build/test:** `dotnet test` runs in Microsoft.Testing.Platform mode (`global.json`); VSTest flags such as
  `--logger` make it exit with code 5 ("Handshake failures", "Zero tests ran"). Single tests:
  `dotnet test --project tests/<Project> --no-build --filter-method "*.TestName"` (repeatable, OR-ed; verified
  2026-09-24). Step 4 of the development loop still shows `--filter <name>`: check whether that form works before
  writing the core.
- **Core, formatting:** `sed -i` rewrites files with bare LF line endings, which the analyzers reject as IDE0055; edit
  with the editor tools, or run `dotnet format` on the project afterwards.
- **Core, shells:** Git Bash rewrites arguments that start with `/` before passing them to native executables (MSYS
  path conversion), so `git grep '///'` searched for `//`.
- **`packages.md`:** a consumer's `packages.lock.json` keeps a package that a referenced project dropped; fix with
  `dotnet restore --force-evaluate`.
- **`claude-config.md`:** workspace trust and the double registration are already in AGENTS.md and move with the
  section.
- **The dotnet-inspect skill:** its doc shows `--oneline`, which the installed tool rejects; fix the skill instead of
  adding a rule.

Personal preferences (never stage without asking, follow a prescribed command form verbatim) stay personal.

## Knowledge that stays in Outline

Cross-project and operational knowledge stays in the wiki at docs.spatial-focus.net; the core only points at the pages
agents need. Semantic search would not change that: full-text search found the right pages on the first try, and no
search helps with a rule nobody knows to look for. What agents lack is the pointer:

- [Infrastructure – Überblick](https://docs.spatial-focus.net/doc/infrastructure-uberblick-EnrCjXS1oh): the two
  clusters, everything deployed through Flux from `SpatialFocus/Infrastructure`
- [Flux CD (Gitops)](https://docs.spatial-focus.net/doc/flux-cd-gitops-K545MSEJBy): the GitOps setup and the image
  automation from `focus.azurecr.io`
- access: the `spatial-focus-docs` MCP server from `.mcp.json`, one sign-in per developer; reads are allowlisted,
  writes ask

## Steps

1. Create the ten rule files by **moving** the text out of AGENTS.md, wording unchanged, then review both for
   duplicates: the docs warn that when two instructions contradict each other, Claude may pick one arbitrarily.
2. Move `.agents/comments-aspire.md` into `csharp.md` and drop the `@.agents/comments-aspire.md` import;
   `.agents/skills` stays.
3. Rewrite the core to the list above, add the index and the Outline pointers, promote the memory findings.
4. Check: `/context` at session start lists CLAUDE.md with AGENTS.md and no rule file; after reading
   `src/admin/FocusTemplate.Admin.Web.Bff/Program.cs`, `authentication.md` is listed too. The build is unaffected,
   the change is documentation only.
5. Delete the promoted entries from the private memory, so they are not maintained twice.

## Open decisions

- **`csharp.md` scoped or global:** it loads in almost every coding session anyway; scoping only saves the sessions
  without code.
- **Other agents:** if colleagues use Codex, Copilot or Cursor, decide whether the AGENTS.md index is enough or the
  rules get mirrored into those tools' formats.
- **Size of a single rule:** `authentication.md` would be about 80 lines of dense prose. The 200-line guidance applies
  per file, so it fits, but the move is a chance to tighten it.

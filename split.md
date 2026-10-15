# Splitting AGENTS.md into a core, docs and rules — plan

*Rewritten 2026-10-07, replacing the rules-only plan of 2026-09-25. The loading behavior below was checked that day
against https://code.claude.com/docs/en/memory. Carried out the same day, see **Outcome** at the end.*

## Why

Every Claude Code session loads AGENTS.md (836 lines, through the `@AGENTS.md` import in CLAUDE.md) and the Aspire
comments guide it imports (`.agents/comments-aspire.md`, 114 lines): about 85 KB before the first message, most of it
about areas the session never touches. The docs target under 200 lines per instruction file.

AGENTS.md also mixes two kinds of text. Most of it explains how something works and why (the BFF flow, the outcome
pattern, the forms machinery); a smaller part says what to do. Explanations belong where people and every agent find
them. Directives belong where Claude has them in context while it edits the files they are about.

## Three layers

| Layer | Holds | Read by | Loads | Size |
|---|---|---|---|---|
| `AGENTS.md` | commands, the development loop, what spans areas, the index | every session, every agent | at session start | 150–180 lines |
| `docs/<area>.md` | how an area works and why | people, every agent | when something points there | as long as needed |
| `.claude/rules/<area>.md` | what to follow when touching the area's files | Claude Code | when Claude reads, edits or writes a matching file | 10–40 lines |

**Each statement lives once.** A rule says what to do, without the reasoning, and ends with a link to its doc. The
doc explains, links to its rule file for the list of what to follow, and does not repeat that list. The rule files
are plain Markdown in the repository, so people read them through that link.

## How the layers load

- **The core**: CLAUDE.md imports AGENTS.md, both load at session start.
- **Rules without `paths:`** are found recursively under `.claude/rules/` and load at session start like CLAUDE.md.
- **Rules with `paths:`** load when Claude uses Read, Write or Edit on a matching file, not on Glob or Grep hits. A
  search over the files does not load the rule; a rule never fires before a command such as `dotnet test`.
- **Glob syntax**: a YAML list or a comma-separated string, brace expansion allowed, at most 1,000 expanded patterns per
  rule. `paths` is the only frontmatter field read; others are ignored. Frontmatter that does not parse makes the rule
  load at session start.
- **`@` imports inside rules are not documented.** Rules link to their doc by path, and Claude reads it when the work
  needs the background.
- **Docs** never load by themselves.
- **Other agents** (Codex, Copilot, Cursor) read AGENTS.md, not `.claude/rules/`. The index in AGENTS.md points them
  at docs and rules. Nested AGENTS.md files are no alternative here: Claude Code reads AGENTS.md only when no CLAUDE.md
  exists, and ours imports it.
- **Checking**: `/context` lists the loaded instruction files under *Memory files*; the `InstructionsLoaded` hook can log
  when and why a file loaded.

## The dividing lines

- **Core or rule:** knowledge an action needs before any file is read stays in the core. "Never run `dotnet format` on
  the MAUI project" has to be in context before the command, not after reading a MAUI file.
- **Rule or doc:** a rule is a directive someone can check in a diff ("a new proxied API needs its own route entry").
  Everything that explains why, or how parts work together, is doc.
- **Rule only:** where an area's text already consists of directives (comment style, packages, Claude configuration),
  there is no doc.

## Areas

Globs leave out the `FocusTemplate.` prefix (`src/admin/*.Admin.Web.Bff/**`), so the fork's rename keeps them valid.

| Area | Doc | From AGENTS.md | Rule paths |
|---|---|---|---|
| `architecture` | yes | the paragraphs per project under **Projects** (the core keeps one line each), **Tech stack** apart from what an area takes | none: doc only |
| `authentication` | yes | **Authentication** (120 lines), the authentication packages | `src/admin/*.Admin.Web.Bff/**`, `src/admin/*.Admin.Api/Authentication/**`, `src/admin/*.Admin.Web/{App.razor,_Imports.razor}`, `src/admin/*.Admin.Web/Infrastructure/Authentication/**`, `src/admin/*.Admin.Web/Foundation/Shell/MainLayout.razor`, `src/admin/*.Admin.Web.ClientServiceDefaults/{CsrfHeaderHandler,RedirectToLoginHandler,ServiceCollectionExtensions}.cs`, `src/admin/*.Admin.Shared/{BffClaimTypes,UserClaim,UserInfoResponse}.cs`, `src/*.AppHost/{AuthenticationExtensions.cs,keycloak/**}`, `tests/*.Admin.Web.E2E/{AuthenticationTests,BlazorAppFixture,KeycloakAdmin}.cs`, `tests/*.IntegrationTests/{AuthenticationTests,TestAuthenticationHandler}.cs` |
| `authorization` | yes | **Authorization** (53) | `src/admin/*.Admin.Api/Authorization/**`, `src/spec/permissions/**`, `src/*.Data/Entities/{Group,GroupMember,GroupPermission,PermissionDefinition,User,UserActivity}*.cs`, `src/admin/*.Admin.Web/Infrastructure/Authorization/**` |
| `spec-first` | yes | **Spec-first APIs** (69), the TypeSpec, Mediator and Vogen lines, the Primitives and emitter paragraphs, **Shared DTOs** | `src/spec/**`, `src/*/spec/**`, `**/tspconfig.yaml`, `**/generated/**`, `**/openapi.yaml`, `src/*.Primitives/**`, `src/*/*.{Client,Shared}/**`, `src/*/*.Api/Features/**`, `package.json`, `scripts/gen.mjs`, `.npm/**` |
| `database` | yes | **Database & migrations** (74), the Data paragraph, the EF lines, **Read/write split**, the query half of **AOT-ready** | `src/*.Data/**`, `src/*/*.Api/Features/**/*Handler.cs`, `tests/*.ArchitectureTests/**` |
| `integration-tests` | yes | **Integration test database** (22), the integration level of the development loop | `tests/*.IntegrationTests/**` |
| `web-pages` | yes | **Pages and view models** (66), the Foundation part of the Web paragraph | `src/admin/*.Admin.Web/Features/**`, `src/admin/*.Admin.Web/Foundation/{Pages,Feedback,Shell}/**`, `src/admin/*.Admin.Web/Foundation/*.cs`, `tests/*.Admin.Web.UnitTests/Features/**` |
| `web-forms` | yes | **Forms** (60) | `src/admin/*.Admin.Web/Foundation/{Forms,Validation}/**`, `src/admin/*.Admin.Web/Features/**/{Form,FormValidator,Mapper}*.cs`, `src/admin/*.Admin.Shared/*Rules.cs`, `tests/*.Admin.Web.UnitTests/Foundation/**` |
| `web-e2e` | yes | the Aspire-system and UI levels and **UI bugs** of the development loop, `data-testid`, how the fixtures work | `tests/*.Admin.Web.E2E/**`, `src/admin/*.Admin.Web/**/*.razor` |
| `apphost` | yes | the AppHost paragraph, orchestration and observability, **Feature flags** | `src/*.AppHost/**`, `src/*.ServiceDefaults/**` |
| `mobile` | yes | the MAUI line, **Mobile bugs/features**, the mobile test level, **Mobile dev loop**, `AutomationId` | `src/public/*.Public.Mobile*/**`, `tests/*.Public.Mobile.E2E/**` |
| `csharp` | no | **Comments** with `.agents/comments-aspire.md` moved in (keeping its source line), the rest of **AOT-ready**, **Extension classes** | `**/*.cs`, `**/*.razor` |
| `packages` | no | Central Package Management, lock files, the EF stack pinned to one version, `Microsoft.Maui.Core` matching the workload band | `Directory.{Packages,Build}.props`, `global.json`, `**/*.csproj`, `**/packages.lock.json`, `.config/dotnet-tools.json` |
| `claude-config` | no | **Avoid permission prompts**: allowlist matching, workspace trust, the double `C:\` / `C:/` registration, settings loaded at session start, the MCP servers | `.claude/**`, `.mcp.json` |

A handler file loads `spec-first` and `database`, a form file `web-pages` and `web-forms`; that is where those areas
meet.

## What stays in AGENTS.md

- the intro and the project map, one line per project
- the essentials of the stack: .NET 11 preview, Central Package Management, analyzers as build errors
- **Running**, including "stop the AppHost before `dotnet build`/`dotnet test`"
- **Build, test, format**: `npm run gen`, build, test with its filter syntax, `dotnet format`, "never format the MAUI
  project", "no `sed -i` on source files"
- the **Agent toolbox** table, including "check the Aspire docs before recommending Aspire changes"
- **Development loop**: the seven steps, one line per test level pointing at its doc, runtime and scale-out bugs
- one line each for what spans areas: an API change goes spec → `npm run gen` → handler → consumers; consumers use the
  generated clients and never hand-write HTTP; nothing under `generated/` is edited by hand; prefer allowlisted single
  commands
- the index: each doc and rule with one line on what it covers, the planning notes at the root (backlog.md,
  lamama.md and the others), and the Outline pages below

## Promote from private agent memory

Findings that live only in one developer's Claude memory (`~/.claude/projects/<repo>/memory/`), so no colleague's
agent knows them:

- **Core, testing:** `dotnet test` runs in Microsoft.Testing.Platform mode (`global.json`); VSTest flags such as
  `--logger` make it exit with code 5 ("Handshake failures", "Zero tests ran"). Single tests:
  `dotnet test --project tests/<Project> --filter-method "*.TestName"`; check whether step 4's `--filter <name>` works
  before writing the core.
- **Core, testing:** unit and integration tests run while the AppHost runs with `--artifacts-path <folder>`, which
  sidesteps the locked binaries; not the E2E suite.
- **Core, testing:** `dotnet test --no-build` after a failed build runs the previous binaries and can report green.
- **Core, formatting:** `sed -i` rewrites files with bare LF line endings, which the analyzers reject as IDE0055.
- **Core, shells:** Git Bash rewrites arguments that start with `/` before passing them to native executables, so
  `git grep '///'` searched for `//`.
- **`packages` rule:** a consumer's `packages.lock.json` keeps a package a referenced project dropped; fix with
  `dotnet restore --force-evaluate`.
- **`spec-first` doc:** an emitter bump runs `npm run gen` and `npm test` in the emitter first (no tracked changes),
  then `npm pack` into `.npm/`, `npm install` of the tgz, `npm run gen` here.
- **The dotnet-inspect skill:** its doc shows `--oneline`, which the installed tool rejects; fix the skill.

Afterwards these entries leave the memory, and so do the two comment-style memories that AGENTS.md's **Comments**
already covers. Personal preferences (never stage without asking, follow a prescribed command form verbatim) stay
personal.

## Knowledge that stays in Outline

Cross-project and operational knowledge stays in the wiki at docs.spatial-focus.net; the core points at the pages
agents need:

- [Infrastructure – Überblick](https://docs.spatial-focus.net/doc/infrastructure-uberblick-EnrCjXS1oh): the two
  clusters, everything deployed through Flux from `SpatialFocus/Infrastructure`
- [Flux CD (Gitops)](https://docs.spatial-focus.net/doc/flux-cd-gitops-K545MSEJBy): the GitOps setup and the image
  automation from `focus.azurecr.io`
- access: the `spatial-focus-docs` MCP server from `.mcp.json`, one sign-in per developer; reads are allowlisted,
  writes ask

## Steps

Two passes, so each review stays readable.

1. **Move.** Each section goes into its doc with its wording unchanged; AGENTS.md shrinks to the core and the index.
   The diff shows moves only. `.agents/comments-aspire.md` moves into the `csharp` rule, and the import goes.
2. **Extract.** From each doc, the directives go into its rule: imperative, one line each, ending with the link to the
   doc. The doc keeps the explanation and links to the rule. Then read doc and rule side by side for statements in
   both: when two instructions contradict each other, Claude may pick either.
3. **Promote** the memory findings, then delete them from the memory.
4. **Check.** At session start `/context` lists CLAUDE.md and AGENTS.md and no rule; after reading
   `src/admin/FocusTemplate.Admin.Web.Bff/Program.cs`, `authentication.md` too. The build is unaffected.
5. **Backlog §0.2:** `docs/` exists; what remains there is the ADRs (`docs/adr/NNNN-*.md`).

## Open decisions

- **`csharp` scoped or global:** it loads in almost every coding session anyway; scoping only saves the sessions
  without code.
- **Other agents:** if colleagues use Codex, Copilot or Cursor, whether the index in AGENTS.md is enough or the rules
  are mirrored into those tools' formats.
- **Tightening:** the move keeps the wording; shortening the dense sections (authentication, spec-first) is a later
  pass of its own.
- **In the fork:** `mobile` goes with the Public vertical; the other globs keep working after the rename.

## Outcome (2026-10-07)

- AGENTS.md: 202 lines and 15 KB, from 836 lines and 79 KB plus the 6 KB comments guide. Above the 150–180 target
  because of the index table and the promoted test notes.
- Eleven docs under `docs/` (architecture plus one per area) and thirteen rules under `.claude/rules/`, each rule
  between 15 and 38 lines apart from `csharp` (154, most of it the Aspire comments guide). Every glob matches files.
- Pass 1 moved the text line by line; only references to AGENTS.md sections were turned into links.
- In this session the `claude-config` rule loaded when a file under `.claude/` was written, and `csharp` when a `.cs`
  file was read.
- `csharp` stays scoped to `**/*.cs` and `**/*.razor`.
- `.agents/comments-aspire.md` is gone, its text is in the `csharp` rule. The dotnet-inspect skill (both copies) no
  longer recommends `--oneline`. The promoted memory entries are deleted.
- **Deviation:** the docs keep their prose unchanged, so a directive a rule lists often appears in its doc too, inside
  the sentence that explains it. "Each statement once" holds for the explanations; the directives exist twice, one line
  in the rule and in context in the doc. The tightening pass is where docs can drop what their rule already says.
- **Still to check:** `/context` in a fresh session (CLAUDE.md and AGENTS.md at start, no rule; `authentication.md` after
  reading a BFF file).

## Cleanup pass (2026-10-07)

Four read-only agents checked every concrete claim in docs, rules and core against the code; the stale ones are fixed
and what an agent sees in the code at a glance is cut. AGENTS.md is at 173 lines and 12 KB, the `csharp` rule at 41.
The move had dropped AGENTS.md's last line (837 lines, no final newline, counted as 836), now restored in
`docs/integration-tests.md`. Deviations of the code from the documented conventions are listed in backlog §0.3.

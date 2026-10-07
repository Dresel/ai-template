---
paths:
  - ".claude/**"
  - ".mcp.json"
---
# Claude Code configuration

- The allowlist in `.claude/settings.json` matches a compound command segment by segment: a pipeline runs without a
  prompt when every segment matches an allow rule and none matches a deny rule. Deny always wins. The `Select-String`,
  `Select-Object` and `ConvertFrom-Json` entries exist so that `dotnet build * | Select-String *` runs unprompted.
- Workspace trust gates the project allowlist. In an untrusted workspace every allow rule is ignored while read-only
  commands still run through built-in heuristics, so the symptom is that only mutating commands (build, format, test)
  prompt. Trust is per-user state in `~/.claude.json`, and on Windows one folder can be registered twice (`C:\…` and
  `C:/…`, CLI and desktop app) with separate flags: check `hasTrustDialogAccepted` on both entries.
- Settings load at session start: restart the session after editing a settings file.
- Rules under `.claude/rules/` load when a file matching their `paths:` is read, edited or written, and `/context` lists
  the ones loaded.
- The Aspire skills, `dotnet-inspect` and `playwright-cli` in `.claude/skills/` and `.agents/skills/` are written by
  `aspire agent init` and change only through it (**Updating Aspire** in [docs/apphost.md](../../docs/apphost.md)).
  The project's own skills (`feature-slice`) live in `.claude/skills/` alone and are maintained by hand.
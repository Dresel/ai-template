---
paths:
  - "**/*.cs"
  - "**/*.razor"
---
# C#

## Comments

- A comment says why, in one line, two at most. Longer reasoning goes into the area's doc under `docs/` or the commit
  message.
- Comment what a reader cannot see: concurrency hazards, lifecycle constraints, platform quirks, a deliberate deviation
  from the obvious API. Code that parses a format shows an example of the raw input. Code that follows an external
  standard links it. A workaround names its upstream issue and the condition for removing it.
- No `///` on hand-written C#. The exceptions are TypeSpec `/** */` and hand-written partials on generated wire or
  client types, whose XML reaches consumers.
- Nothing under `generated/` or `Migrations/` is commented.
- A comment describes the code as it stands, never how it came about ("as discussed", "was previously X").
- Say it plainly: state the reason itself ("Loaded again after the change, so the page shows what the server stored"),
  without a strawman contrast ("rather than a guess", "X, not Y", "here rather than in X") and without clauses chained
  by semicolons.
- Say it once: a pattern several files follow is explained in its doc, and each file comments only what is particular
  to it.

Adapted from the [Aspire repository's comment guide](https://github.com/microsoft/aspire/blob/main/AGENTS.md#code-comments).

## AOT readiness

Nothing publishes NativeAOT yet, but switching must stay a publish setting.

- Register by hand: a new `IEntityTypeConfiguration<T>` goes into `ApplyEntityConfigurations`, a new service into its
  feature's extension method. No reflection-based discovery.
- An API marked `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]` is a stop sign. Prefer source generators.

## Extension classes

- Named after the type they extend, without an interface's `I` (`ServiceCollectionExtensions`,
  `ProjectResourceBuilderExtensions` for `IResourceBuilder<ProjectResource>`), or after the constraint of a generic
  receiver (`TBuilder : IHostApplicationBuilder` → `HostApplicationBuilderExtensions`).
- Classes that map endpoints are named after the endpoints (`DebugEndpoint`), and so is a class whose methods belong to
  one feature and only use the receiver as entry point (`GroupQueries`, `Administrators`).
- One class per extended type and namespace. The namespace says what the methods are about, the method name what they
  do. Names that came with a template stay (`Extensions` in both ServiceDefaults projects, `BlazorClientExtensions`).
# AOT readiness map

*Measured 2026-10-02 against SDK 11.0.100-rc.1.26425.128, EF Core 10.0.12 (and 11.0.0-rc.1.26425.128 in a scratch
copy), Npgsql 10.0.3 (11.0.0-rc.1.1), Vogen 8.0.7. Nothing here publishes NativeAOT; this records how far the way
there is open, so the AOT-ready rule in `.claude/rules/csharp.md` knows what it buys today.*

## Status

| Piece | EF 10.0.12 | EF 11.0.0-rc.1 | What blocks it |
|---|---|---|---|
| Compiled model (`dbcontext optimize`) | works | works | nothing; the typed-id sentinels (see **Typed ids as keys** in `docs/database.md`) made it work, `ModelSentinelTests` keeps it so |
| NativeAOT compiled model (`--nativeaot`) | fails | fails | EF writes the sentinel comparisons as C# and emits only primitive constants: `Encountered a constant of unsupported type 'AlertId'` |
| Precompiled queries (`--precompile-queries`) | 0 of 27 | 0 of 27 | the three hurdles below, the third with no workaround worth adopting |

The NativeAOT compiled model has no local fix: a sentinel EF could write would be the CLR default, and the converter
would then have to accept an uninitialized id, which Vogen forbids by design. It needs EF to write a non-primitive
sentinel through its converter; no issue is filed yet.

## Precompiled queries, hurdle by hurdle

1. **The tooling's Roslyn predates `union`.** EF Design 10.0.12 brings Microsoft.CodeAnalysis 5.0, 11.0.0-rc.1 brings
   5.3; neither parses the C# 15 unions the emitter generates for result types, so the tool's own compilation stops at
   546 errors before any query. Works around: `Microsoft.CodeAnalysis.CSharp.Workspaces` and
   `Microsoft.CodeAnalysis.Workspaces.MSBuild` 5.9.0 referenced by the startup project.
2. **The workspace needs reflection-based JSON.** From Roslyn 5.3 on, the MSBuild workspace talks to its build host
   through reflection-based System.Text.Json, while the tool runs under the startup project's runtime configuration,
   and the Admin API sets `JsonSerializerIsReflectionEnabledByDefault` to false: "Reflection-based serialization has
   been disabled". Works around: the setting on for the tool run, or a separate startup project for the tooling, which
   leaves the API's off.
3. **Parameters in a query** ([efcore#35887](https://github.com/dotnet/efcore/issues/35887), open, milestone EF 12;
   [efcore#38030](https://github.com/dotnet/efcore/issues/38030) closed as its duplicate). A context that reaches the
   query as a parameter, through a primary constructor (the handlers, `UserPermissions`) or a method
   (`UserProvisioningMiddleware`, `GroupQueries`, `Administrators`), makes it "Dynamic LINQ queries are not supported
   when precompiling queries"; a method parameter inside the query (`subject`, `cancellationToken`) fails with
   `UnreachableException: IdentifierName of type ParameterSymbol`. Copying the context and every parameter into locals
   takes a query through the analysis (tried on one handler); whether its generated code is right is unverified, since
   EF writes nothing while any query fails. Locals in every query are no style for the template, so this waits for the
   fix.

## List queries

A list that searches, sorts and pages is built across statements (the exception under **Queries** in `docs/database.md`), so EF counts
it as dynamic even once the hurdles are gone. EF's documentation expects dynamic queries to run under NativeAOT
eventually, without precompilation and slower, so such a list costs speed at the move to NativeAOT, not a rewrite.

## Measuring again

No database is needed; the connection strings only satisfy the startup project's registration:

```powershell
$env:ConnectionStrings__focusdb = "Host=localhost;Database=focusdb"
${env:ConnectionStrings__focusdb-readonly} = "Host=localhost;Database=focusdb"
dotnet dotnet-ef dbcontext optimize --project src/admin/FocusTemplate.Admin.Api --startup-project src/admin/FocusTemplate.Admin.Api --context ReadOnlyAppDbContext --precompile-queries --output-dir <a folder outside the repo> --verbose
```

`--nativeaot` adds the NativeAOT model, `--no-scaffold` skips the model. Keep the output outside the repository: the
generated model and interceptors are not checked in, and are stale after any change. Worth measuring again with the
first EF 12 preview, a fix of efcore#35887, or an EF Design whose Roslyn knows `union`.

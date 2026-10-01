# Typed identifiers — Vogen findings, the generator-visibility finding, and what landed

*Compiled 2026-09-21, updated 2026-09-22 after the implementation. Measured against Vogen 8.0.7, EF Core 10.0.12,
Npgsql 10.0.3, .NET 11 and Postgres 17; the probes were scratch projects and are not in the repo.*

Two questions were open around typed ids: whether **Vogen** should own them (a decision from 2026-07-14), and how a
single id could be **shared** between the domain (`FocusTemplate.Data`) and the per-vertical wire contracts. Both are
answered and implemented. This file records the measurements, the finding that decided the layout, and the result.

## Where we are today (2026-09-22)

- **One id, one assembly.** `src/spec/primitives.tsp` declares `@typedId scalar WeatherForecastId extends int32;` in
  namespace `FocusTemplate.Primitives`, no service. The new project `FocusTemplate.Primitives` compiles it with
  `output-type: primitives` and gets one Vogen struct per id, in the scalar's own namespace:

  ```csharp
  [ValueObject<int>]
  [Instance("Unspecified", 0)]
  public readonly partial struct WeatherForecastId;
  ```

  `Data` and `Admin.Shared` reference the project; the Admin `api.tsp` imports the spec and references the id by
  namespace. Entity, request record, response record and client all use the same type; the handlers compare
  `forecast.Id == query.Id` and pass `entity.Id` through.
- **EF wiring** in `Data`: `VogenEfCoreConverters` with `[EfCoreConverter<WeatherForecastId>]`, registered through the
  generated `RegisterAllInVogenEfCoreConverters()`; the entity initializes `Id = WeatherForecastId.Unspecified`, the model
  declares `ValueGeneratedOnAdd().HasSentinel(WeatherForecastId.Unspecified)`. No migration: the column is unchanged,
  `has-pending-model-changes` says so.
- **Verified**: emitter suite 165 + 30 tests, Admin integration tests 6/6 against Postgres including two inserts from
  the sentinel receiving distinct ids, the whole solution builds. Option C from the earlier plan (a shared primitives
  project) is what landed, for a reason the plan did not know yet: see [generator visibility](#the-finding-that-decided-the-layout).

## Vogen findings

### The question

Vogen was the decided mechanism for strongly-typed ids (the 2026-07-14 decision line and §2.3), and the idea on the
table was to have the emitter emit `[ValueObject<T>]`-decorated ids, so one Vogen struct could serve the domain and
the contract at once. That needs a Vogen struct to work as an **EF Core key**, including under value generation.

**It does — but only if the key property is never left uninitialized.** The naive shape fails, and the fix is two
lines of ceremony per id.

### What was measured

Every row is a store-generated or generated key, unless stated otherwise.

| Key model | Result |
|---|---|
| `[ValueObject<int>]`, `ValueGeneratedOnAdd()`, key left unset | **`ValueObjectValidationException: Use of uninitialized Value Object`** in `db.Add`, before any SQL |
| the same plus `HasSentinel(ForecastId.From(0))`, key still left unset | **same throw, same place**, 0 rows |
| `[ValueObject<Guid>]`, `ValueGeneratedOnAdd()` + client-side UUIDv7 `ValueGenerator`, key left unset | **same throw** in `db.Add`: EF reads the unset key *before* it calls the generator |
| `[ValueObject<int>]` + `[Instance("Unset", 0)]`, property initialized `= ForecastId.Unset`, `ValueGeneratedOnAdd()` + `HasSentinel(ForecastId.Unset)` | **works** — ids 1, 2, 3 assigned by the identity column, query by id round-trips |
| the same **without** `HasSentinel` | **silently inserts id 0** — no exception. EF reads an initialized value and treats it as explicit, skipping identity generation. A second insert would collide on the primary key |
| `[ValueObject<Guid>]` + sentinel instance, `ValueGeneratedOnAdd()` + `HasValueGenerator<…>()` + `HasSentinel(…)` | **works** — the generator is called, `01a0c5a0-…`, `Version` 7 |
| `[ValueObject<int>]` with `Validate(value > 0)`, sentinel + `HasSentinel`, `ValueGeneratedOnAdd()` | **`ValueObjectValidationException: An id must be positive.`** in `db.Add` — EF assigns *temporary negative* key values and they go through the converter |
| `ValueGeneratedNever()`, id assigned before `Add` (int 4711, or UUIDv7 minted by the entity) | **works**, round-trips |
| translation of `forecast.Id == id` | `WHERE f."Id" = 1`, identical to the plain record struct — querying was never the problem |
| `ForecastId.From(0)` without validation | allowed, so zero alone cannot mean "not set"; that is what `[Instance]` + `HasSentinel` declare |
| a fresh entity's `.Id.Value` when the property has no initializer | throws |

### Why the naive shape fails

A struct can always be `default`-constructed, so Vogen puts a `ThrowHelper.ThrowWhenNotInitialized` in the `Value`
getter. EF has to read the key through the value converter (`id => id.Value`) to decide whether it must generate one
— for a store-generated column, for a sentinel comparison, and for a client-side `ValueGenerator` alike. With no
property initializer, every one of those paths hits that getter on an uninitialized struct.

### The two lines that make it work

1. **A sentinel instance, and a property initializer that uses it.** `[Instance("Unspecified", 0)]` on the value
   object, `public ForecastId Id { get; set; } = ForecastId.Unspecified;` on the entity. `[Instance]` bypasses
   `Validate`, so the sentinel may be a value the id otherwise rejects.
2. **`HasSentinel(ForecastId.Unspecified)`** on the key property, so EF knows which value means "not set".

Leaving out (2) is the dangerous variant: no exception, an explicit `0` written to an identity column, and a primary
key collision on the next insert. This is exactly what `AppDbContext` and the entity do today.

### The one real limitation

**A validated id cannot be a store-generated integer key.** EF assigns temporary negative values during `Add` and
converts them through `From(…)`, which runs `Validate`. An id that rejects non-positive values therefore throws
before any SQL. Client-side generation (UUIDv7 via a `ValueGenerator`) has no temporary-value phase and is
unaffected, so a validated id is fine there. Vogen's generated EF converter reads through the deserialization path,
which returns known instances such as `Unspecified` without validating them; a hand-written `From(value)` converter
would not.

### There is no opt-out for the uninitialized guard

The full `VogenDefaultsAttribute` surface has no switch that disables it. `IsInitializedMethodGeneration` only
generates an `IsInitialized()` *query*, and `disableStackTraceRecordingInDebug` is a perf knob. The analyzer goes
further: `default(ForecastId)` is compile error **VOG009** and `new ForecastId()` is **VOG010**, and both fire in every
project that references the ids through a project reference, not only where the Vogen package is referenced
(measured 2026-09-22). Observing the behaviour at all required laundering through
`static T Uninitialized<T>() => default!`. `Equals` on an uninitialized value returns `false` without throwing, and
`ToString()` returns `[UNINITIALIZED]`.

### Vogen is not generator-only

`Vogen.SharedTypes.dll` lands in the build output, and the generated code needs three of its types at run time
(`ValueObjectValidationException`, `Validation`, `ValueObjectOrError`). Emitting Vogen ids therefore adds that runtime
assembly to every consumer, the Blazor WASM payload and the MAUI app included. Accepted on 2026-09-22.

### Two more Vogen facts that shaped the emitter

- **The `Instance` value is written verbatim into the generated source.** `[Instance("Unspecified",
  "00000000-0000-0000-0000-000000000000")]` on a `Guid` id comes out as `new WidgetId(00000000 - 0000 - …)`, CS1503.
  A Guid sentinel needs a C# expression; the emitter writes `"global::System.Guid.Empty"`.
- **Vogen's System.Text.Json converter goes through the options.** On .NET 5+ it calls
  `JsonSerializer.Serialize(writer, value.Value, options)` and `options.GetTypeInfo(typeof(long))`. With
  `JsonSerializerIsReflectionEnabledByDefault=false` the generated context therefore has to register `int`, `long`
  and `Guid` beside the ids, which the emitter now does. Consequence: a quoted number `"orderNumber": "42"` is accepted
  under the web defaults, like it is for the `long` the id wraps; the old hand-written converter refused it.

## The finding that decided the layout

**Source generators do not see each other's output.** The System.Text.Json generator behind the contracts'
`JsonSerializerContext` runs on the same compilation as Vogen. When an id and the context sit in one project, STJ
sees a struct with neither `[JsonConverter]` nor `Value` (both are Vogen output) and models it as an empty object:
`{"id":{}}` on the wire. Vogen's `VogenTypesFactory` does not help there either, `[JsonSourceGenerationOptions(Converters
= …)]` names a type STJ cannot resolve in the same run and drops it silently (SYSLIB1220 at best).

Across assemblies it works: STJ reads the `[JsonConverter]` from the metadata of the referenced Primitives assembly
and emits `CreateValueInfo` with Vogen's converter. No factory, no runtime registration.

That is why the ids have a project of their own, and why the emitter refuses a `@typedId` inside a service namespace
(`typed-id-in-service-namespace`): "local" typed ids in a `Shared` project would compile and then serialize as `{}`.
The layout is not a taste decision but the only one that works with reflection-free serialization.

Two further measurements from the same probe: the parent emitter skips generation for a program without `@service`
because it has no root namespace (microsoft/typespec#10914), but takes a `namespace` override, which the wrapper
sets for a primitives run; and the code model carries no scalars at all, so an unreferenced `@typedId` never reached
the generator. The typed ids now travel as a `typed-ids` option filled from the decorator state, with the scalar's
doc comment, which the code model also dropped.

## What was built

Emitter (`typespec-http-csharp-slim`):

- `output-type: primitives` writes every `@typedId` of the program as a Vogen struct into the scalar's TypeSpec
  namespace, nothing else; the contracts run never writes ids and references them by that namespace.
- `typed-ids` option from the TypeScript half; `typed-id-in-service-namespace` diagnostic (the service namespace
  itself, child namespaces such as `Contoso.Admin.Ids` are allowed); backing primitives registered in the context.
- Scenario `typed-ids` with `ids.tsp` + `primitives/` sub-project, `Scenarios.Primitives` as the only project running
  Vogen; the integration host sets `RespectRequiredConstructorParameters`, and a body without the id is a 400.

Template:

- `src/spec/primitives.tsp`, `FocusTemplate.Primitives`, `gen.mjs` rule for projects directly under `src/`
  (`src/spec/<name>.tsp`), `@friendlyName` on the scalar so the OpenAPI schema stays `WeatherForecastId` rather than
  the namespace-qualified name openapi3 gives a type outside the service namespace.
- `Data`: marker, sentinel, initializer; `Admin.Api`: `RespectRequiredConstructorParameters`; handlers without the
  mapping hop; integration test for distinct store-generated ids.

## Open

- Public carries no id yet; `id: WeatherForecastId` in its response would prove the sharing through MAUI.
- `RespectRequiredConstructorParameters` as the default of the generated client's options.
- When ids become self-assigned (UUIDv7 in the aggregate, `ValueGeneratedNever()`), the sentinel ceremony disappears
  for those and validation becomes unrestricted.
- Per-vertical primitives (`FocusTemplate.Admin.Primitives` beside `Admin.Shared`) are possible without emitter changes;
  the only fixed rules are a separate assembly from the context and no id in the service namespace itself.

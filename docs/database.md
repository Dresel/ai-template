# Database

What to follow when changing these files: [the rule](../.claude/rules/database.md).

## The project

`FocusTemplate.Data` holds the entities with their EF Core mapping (`Entities/`, each entity next to its
`IEntityTypeConfiguration`, all listed by hand in `ApplyEntityConfigurations`), auditing (`Auditing/`), the model in
`AppDbContextBase` with its two sealed contexts, the migrations, a design-time factory and the dev seed. Both APIs and
the AppHost's migration resource reference it, and it stays free of Aspire.

- **No DDD layer.** Entities are plain classes (`required`/`init`, public setters, no base types, no domain events).
  Business rules (lifecycle, ownership, thresholds) live in the Mediator handler that needs them, which answers a refusal
  with a case of its result union (see **Errors are values** in [spec-first](spec-first.md)).
- **Postgres** runs the `postgis/postgis` image: a station's location is a `geography` point through NetTopologySuite,
  created as `new Point(longitude, latitude) { SRID = 4326 }`. Longitude comes first, and swapped arguments still
  compile. `StationStatus` and `AlertKind` are native Postgres enums.
- **Provider setup**: `ConfigureAppDbContext` and `ConfigureReadOnlyAppDbContext` give every context its provider with
  `MapEnum`, the naming and the check-constraint plugins. A bare `UseNpgsql` would miss them.
- **Registration**: the Admin API registers both pools, `AddAppDbContextPool("focusdb")` and
  `AddReadOnlyAppDbContextPool("focusdb-readonly")`, the Public API only the read-only one, each followed by Aspire's
  `EnrichNpgsqlDbContext<TContext>()` (retries, health checks, OTel).

## Packages

- `Npgsql.EntityFrameworkCore.PostgreSQL` with its NetTopologySuite plugin, `EFCore.CheckConstraints` (validation
  attributes such as `[Range]` become CHECK constraints) and `EFCore.NamingConventions` (snake_case in the database).
- EF Core, its Design and Relational packages and the repo-local `dotnet-ef` tool share one version. The Npgsql provider
  and the two `EFCore.*` plugins follow their own versions.
- `Bogus` (MIT) for the dev seed's made-up users, in `Data` because the seed runs in the migrations tool.
- The AppHost wires the database with `Aspire.Hosting.PostgreSQL` and `Aspire.Hosting.EntityFrameworkCore`
  (`AddEFMigrations`).

## Read/write split

Query handlers (`IQueryHandler`) take `ReadOnlyAppDbContext`, command handlers (`ICommandHandler`) take `AppDbContext`:
a command reading through the second context would lose its transaction and its tracking. `HandlerDbContextTests` in
`FocusTemplate.ArchitectureTests` checks the constructor parameters of every handler in both APIs and forbids the
shared `AppDbContextBase`, so a handler always names the context it means.

The read-only context tracks nothing and refuses `SaveChanges`. It is registered on its own connection string key,
`focusdb-readonly`, meant for the SELECT-only `focusdb_reader` role. The role is what stops `ExecuteUpdate`,
`ExecuteDelete` and raw SQL. Locally the AppHost injects the owner connection under both keys, so a plain
`aspire start` needs no second login. The integration tests run the real role, and a published environment points the
second key at it.

**Reader role**: `focusdb_reader` belongs to the environment. No migration creates it or grants to it: roles are
cluster-level, `CREATE ROLE` needs `CREATEROLE`, which the identity running the bundle usually lacks, and default
privileges set by a migration would bind to whichever role ran it. Every environment that uses the read-only key creates
the login (or grants the role to its own identity) and, once the schema is applied, runs
`GRANT USAGE ON SCHEMA public TO focusdb_reader; GRANT SELECT ON ALL TABLES IN SCHEMA public TO focusdb_reader;`.
Production repeats the grant after a bundle that adds tables, or sets
`ALTER DEFAULT PRIVILEGES FOR ROLE <migrating role> IN SCHEMA public GRANT SELECT ON TABLES TO focusdb_reader` once.

## Queries

Each EF query is one method-syntax chain from the `DbSet` to its terminal operator, because the precompiler cannot
follow a query composed across statements or written in query syntax, and value converters capture no state. A list
that searches, sorts and pages is the exception: it builds its filter once, counts it, picks the order in a switch over
the sort enum and pages after it, since one chain per sort order and direction doubles with every sort key.

## Mapping

- A property's value constraints are annotations on the entity (`[MaxLength]`, `[Precision]`, `[Range]`), and every
  string has a `[MaxLength]`, so no column ends up unbounded `text`. `EFCore.CheckConstraints` turns the attributes EF
  ignores into CHECK constraints, but it silently skips a `[Range]` whose bounds are not of the property's type, and
  `RangeAttribute` has no decimal bounds: a decimal range is a `HasCheckConstraint` in the configuration.
- The entity's `IEntityTypeConfiguration` keeps what is not a constraint on one value: keys, column types,
  relationships, indexes.
- The database uses snake_case (`weather_forecasts.temperature_c`, `pk_`/`fk_`/`ix_`), and raw SQL and `HasCheckConstraint`
  bodies use those names. `__EFMigrationsHistory` keeps its name.

## Typed ids as keys

`VogenEfCoreConverters` carries one `[EfCoreConverter<T>]` per id. `ConfigureConventions` in `AppDbContextBase` calls
the generated `RegisterAllInVogenEfCoreConverters()` and makes each id's `Unspecified` the sentinel of every property of
its type (`Properties<T>().HaveSentinel(T.Unspecified)`), because Vogen refuses the CLR default, an uninitialized id. A
new id needs both lines, and `ModelSentinelTests` fails without the second.

A store-generated key also declares `ValueGeneratedOnAdd()`, and the entity initializes it with `Id.Unspecified`: the
integer-key convention does not reach a key behind a converter, and EF reads the key before generating one. Without
the sentinel, the zero is written into the identity column and the second insert collides.

## Auditing

`IAuditable` entities get four shadow columns (`CreatedAt/By`, `UpdatedAt/By` as `UserId`) from
`AddAuditingShadowProperties`, set by `AuditingInterceptor` from `TimeProvider` and `ICurrentUser`.
`ConfigureAppDbContext` attaches it to the write context, so a host that registers the write pool registers both
inputs. A host that only queries (the Public API) needs neither. `ExecuteUpdate` and raw SQL bypass the interceptor.

`ICurrentUser` is the request's token subject in the Admin API (`HttpContextCurrentUser`, see
[authentication](authentication.md)). Where no person acts, the interceptor records `WellKnownUsers.System`, which the
tooling and the seed name through `FixedCurrentUser`. `WellKnownUsers.Developer` is the id of the realm's `developer`
login, so what the seed attributes to the developer belongs to whoever logs in locally.

## Optimistic concurrency (planned)

No entity carries a row version yet. The concurrency item in the backlog adds it, Group first. The intended shape: an
entity clients edit has `[Timestamp] public uint Version`, which Npgsql maps to Postgres's `xmin` system column and
leaves out of the migration SQL. It goes out in the response and comes back in the update request, and the handler
answers a stale version, or a `DbUpdateConcurrencyException` from its save, with a conflict.

## Migrations

The schema is applied by the `migrations` resource, never by an API, which `WaitForCompletion`s it, so there is no
startup race under scale-out. Locally and in the E2E suite the resource runs `dotnet ef database update` on start.
`aspire publish` emits it as a migration-bundle container.

Adding one, with the AppHost stopped:
`dotnet dotnet-ef migrations add <Name> --project src/FocusTemplate.Data --startup-project src/FocusTemplate.Data --context AppDbContext`.
`--context` is required because the assembly holds two contexts and only `AppDbContext` has migrations. The design-time
`AppDbContextFactory` needs no live database. The migration resource's dashboard commands (Add Migration,
Update/Reset/Drop Database, Status) do the same.

## Dev seed

`DevelopmentSeed` runs `WeatherSeed`, makes the developer an administrator (see [authorization](authorization.md)) and
runs `UserManagementSeed`: 48 made-up users from `Bogus` with a fixed random seed, so they are the same on every start,
and the groups `Support` and `User administration`. None of the made-up users can sign in.

It runs through EF's `UseSeeding`/`UseAsyncSeeding` when the migration tool applies migrations, in run mode only: the
AppHost sets `Database__SeedTestData` on the tool resource through `configureToolResource`, and the published bundle
never seeds. Each part checks for its own rows before inserting, so running it again inserts nothing. Both delegates
are implemented, since the EF CLI calls the synchronous one.

## Data between starts

By default the dev Postgres has no data volume: each `aspire start` and each E2E run gets a fresh, re-seeded database.
`Features:PersistentDatabase` gives the container a persistent lifetime and a data volume, so the migrations find
themselves applied and the seed finds its rows. A migration edited after it was applied, or one removed, is then out of
step with `__EFMigrationsHistory` until `aspire stop --force --volumes` drops container and volume (see
[authentication](authentication.md) for the CLI's version caveat). The E2E fixtures pin the flag off.

## Architecture tests

`FocusTemplate.ArchitectureTests` checks rules every handler or type must follow: `HandlerDbContextTests` reflects over
both API assemblies for the read/write split, `ModelSentinelTests` walks the EF model for the sentinels.
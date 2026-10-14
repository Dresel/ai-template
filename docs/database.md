# Database

What to follow when changing these files: [the rule](../.claude/rules/database.md).

## The project

`SpatialFocus.Data` holds the spine's entities, identity and permissions, with their EF Core mapping (`Entities/`, each
entity next to its `IEntityTypeConfiguration`, all listed by hand in `ApplyEntityConfigurations`), the model in
`AppDbContextBase` with its two sealed contexts, the migrations, a design-time factory and the dev seed. User
management's Api, the Admin API and the AppHost's migration resource reference it, and it stays free of Aspire. Its
tables sit in `public`.

`SpatialFocus.Data.Foundation` holds what any context builds on and no entity of its own: `FoundationDbContext<TModel>`
with `IHasSchema`, auditing (`Auditing/`, with `ICurrentUser` and `WellKnownUsers`), `UseNpgsqlWithConventions` (the
provider with the naming and check-constraint plugins), `AddFoundationConventions` (the `UserId` converter and sentinel
the audit columns need), `IReadOnlyDbContext`, the registration of a feature's contexts, `UserReference` and
`SearchPattern`.

**Every context's model derives from `FoundationDbContext<TModel>`**, `TModel` being the base its writable and
read-only contexts share (`AppDbContextBase`, `WeatherDbContextBase`). It seals EF's two hooks: the foundation's
conventions come first, the default schema next, the context's own model through `ConfigureIds` (its converters and
sentinels) and `ConfigureModel` (its configurations), and `AddAuditingShadowProperties` last, so every `IAuditable`
entity gets its audit columns. The base states its schema as a static member (`IHasSchema.Schema`, `public` for the
spine), since EF builds a model once per type and a pooled context takes nothing but its options. The model and a
feature's registration both read it from there.

A feature's data project, `SpatialFocus.Weather.Data` the first, holds the feature's entities with a context and its
read-only twin of its own, its migrations and its seed, see **Feature contexts**.

- **No DDD layer.** Entities are plain classes (`required`/`init`, public setters, no base types, no domain events).
  Business rules (lifecycle, ownership, thresholds) live in the Mediator handler that needs them, which answers a refusal
  with a case of its result union (see **Errors are values** in [spec-first](spec-first.md)).
- **Postgres** runs the `postgis/postgis` image, which the weather feature needs: a station's location is a
  `geography` point through NetTopologySuite, created as `new Point(longitude, latitude) { SRID = 4326 }`. Longitude
  comes first, and swapped arguments still compile. `StationStatus` and `AlertKind` are native Postgres enums in schema
  `weather`. The spine needs neither PostGIS nor its plugin, so a host without the feature can run plain Postgres.
- **Provider setup**: `ConfigureAppDbContext` and `ConfigureReadOnlyAppDbContext` give the spine's contexts their
  provider through `UseNpgsqlWithConventions`, a feature's contexts get theirs through `ConfigureFeatureDbContext` and
  `ConfigureReadOnlyFeatureDbContext`, with the feature's own types (`UseWeatherTypes`: the PostGIS plugin and the
  enums). A bare `UseNpgsql` would miss them.
- **Registration**: user management, which alone works on the identity tables, registers both spine pools in
  `AddUserManagement(ConnectionNames.Database, ConnectionNames.ReadOnlyDatabase)`: `AddAppDbContextPool` and
  `AddReadOnlyAppDbContextPool`, each followed by Aspire's `EnrichNpgsqlDbContext<TContext>()` (retries, health checks,
  OTel). `AddWeather()` does the same for the feature's two, on the same connections. The Public API registers only
  `AddReadOnlyWeatherDbContextPool(ConnectionNames.ReadOnlyDatabase)`.

## Packages

- `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.CheckConstraints` (validation attributes such as `[Range]` become
  CHECK constraints) and `EFCore.NamingConventions` (snake_case in the database), in `Data.Foundation`. The
  NetTopologySuite plugin only in `Weather.Data`.
- EF Core, its Design and Relational packages and the repo-local `dotnet-ef` tool share one version. The Npgsql provider
  and the two `EFCore.*` plugins follow their own versions.
- `Bogus` (MIT) for the dev seed's made-up users, in `Data` because the seed runs in the migrations tool.
- The AppHost wires the database with `Aspire.Hosting.PostgreSQL` and `Aspire.Hosting.EntityFrameworkCore`
  (`AddEFMigrations`).

## Read/write split

Query handlers (`IQueryHandler`) take a read-only context (`ReadOnlyAppDbContext`, `ReadOnlyWeatherDbContext`), command
handlers (`ICommandHandler`) a writable one (`AppDbContext`, `WeatherDbContext`): a command reading through the second
context would lose its transaction and its tracking. A read-only context implements `IReadOnlyDbContext`.
`HandlerDbContextTests` in `FocusTemplate.ArchitectureTests` checks the constructor parameters of every handler in both
APIs, in user management's and in the features' API assemblies and forbids the abstract bases, so a handler always names
the context it means.

The read-only context tracks nothing and refuses `SaveChanges`: `FoundationDbContext` throws for any context that
implements `IReadOnlyDbContext`, so a twin is its model base plus the interface and has no members of its own. It is
registered on its own connection string key, `focusdb-readonly`, meant for the SELECT-only `focusdb_reader` role. The
role is what stops `ExecuteUpdate`, `ExecuteDelete` and raw SQL. Locally the AppHost injects the owner connection under
both keys, so a plain `aspire start` needs no second login. The integration tests run the real role, and a published
environment points the second key at it.

**Reader role**: `focusdb_reader` belongs to the environment. No migration creates it or grants to it: roles are
cluster-level, `CREATE ROLE` needs `CREATEROLE`, which the identity running the bundle usually lacks, and default
privileges set by a migration would bind to whichever role ran it. Every environment that uses the read-only key creates
the login (or grants the role to its own identity) and, once the schema is applied, runs
`GRANT USAGE ON SCHEMA <schema> TO focusdb_reader; GRANT SELECT ON ALL TABLES IN SCHEMA <schema> TO focusdb_reader;`
for `public` and for each feature's schema (`weather`). Production repeats the grant after a bundle that adds tables,
or sets `ALTER DEFAULT PRIVILEGES FOR ROLE <migrating role> IN SCHEMA <schema> GRANT SELECT ON TABLES TO focusdb_reader`
once per schema. The integration tests grant on every schema of the database.

## Feature contexts

A feature's tables live in a schema named after it, with a history table of its own (`weather.__EFMigrationsHistory`),
so its migrations, its seed and its removal (`DROP SCHEMA weather CASCADE`) never touch another's. Both come from
`WeatherDbContextBase.Schema`: `FoundationDbContext` makes it the model's default schema, and
`AddFeatureDbContextPool`/`AddReadOnlyFeatureDbContextPool` read it from the context type for the history table. Its
native enums name the schema too (`MapEnum<StationStatus>(schemaName: WeatherDbContextBase.Schema)`). The type's name
is the enum's name in snake_case (`station_status`), so renaming the enum renames the Postgres type in the next
migration.

- **The spine's users**: `UserReference` (`Id`, `DisplayName`, `IsActive`) maps `public.users` with
  `ExcludeFromMigrations()`, so a feature's migration creates the foreign key (`Station.OwnerId` →
  `public.users.id`) and its queries join the owner's name, while the table stays the spine's. A feature never saves
  through it. `UserReferenceTests` compares its columns with the spine's `User`.
- **Nothing across features**: a context knows its own tables and the shapes the spine promises (`UserReference`). Two
  features meet through ids and a lookup, never a shared entity.
- **Pooling**: a pooled context's only constructor parameter is its `DbContextOptions<T>`, and the singleton
  `AuditingInterceptor` reads `ICurrentUser` on every save, so a reused instance never carries an earlier request's user
  (`FeatureTests.APooledContextTakesItsOptionsAlone`, `AuditingTests.APooledContextAuditsEachRequestWithItsOwnUser`).

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

`VogenEfCoreConverters` carries one `[EfCoreConverter<T>]` per id (a feature's context has its own class,
`WeatherEfCoreConverters`). `ConfigureIds` in `AppDbContextBase` calls the generated `RegisterAllInVogenEfCoreConverters()` and makes each id's `Unspecified` the sentinel of every property of
its type (`Properties<T>().HaveSentinel(T.Unspecified)`), because Vogen refuses the CLR default, an uninitialized id. A
new id needs both lines, and `ModelSentinelTests` fails without the second. `UserId` is the exception: every model
holds it through the audit columns, so `AddFoundationConventions` brings its converter and sentinel.

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

The schema is applied by the `migrations` resource for the spine and one resource per feature (`weather-migrations`),
never by an API, which `WaitForCompletion`s them, so there is no startup race under scale-out. A feature's resource waits
for the spine's, since its foreign keys point at the spine's users. Locally and in the E2E suite each runs
`dotnet ef database update` on start. `aspire publish` emits each as a migration-bundle container.

The AppHost adds each with `AddMigrations<TContext>(name, migrationsProjectPath, database)` (`Migrations/` in
`SpatialFocus.AppHost.Hosting`), which holds what every migrations resource needs: the tool's environment, both database references, the update on
start and the bundle. A feature's call adds `.WaitForCompletion(migrations)`, and the APIs that read its tables wait for
it in turn.

Adding one, with the AppHost stopped:
`dotnet dotnet-ef migrations add <Name> --project src/SpatialFocus.Data --startup-project src/SpatialFocus.Data --context AppDbContext`,
for the weather feature `--project` and `--startup-project` `src/features/weather/SpatialFocus.Weather.Data` with
`--context WeatherDbContext`. `--context` is required because each assembly holds two contexts and only the writable one
has migrations. The design-time factories need no live database. The migration resources' dashboard commands (Add
Migration, Update/Reset/Drop Database, Status) do the same.

A design-time factory reads its connection and the seed flag through `DesignTime` in `Data.Foundation`.
`AddMigrations` references the database on each migrations resource twice: under its own name, which the published bundle's
Dockerfile reads (`efbundle --connection "$ConnectionStrings__focusdb"`), and under `DesignTime.ConnectionName`
(`migrationdb`), which the factories read. So a feature's factory never names the host's database. Without either, as
for `migrations add`, a factory falls back to a placeholder that builds the model without connecting.

The spine's migrations start over at one `InitialCreate` from the day weather moved into its feature, since the
earlier ones created its tables in `public` and needed PostGIS. A database from before that day is dropped, not
migrated: `aspire stop --force --volumes` with `Features:PersistentDatabase` on.

## Dev seed

`DevelopmentSeed` makes the developer an administrator (see [authorization](authorization.md)) and runs
`UserManagementSeed`: 48 made-up users from `Bogus` with a fixed random seed, so they are the same on every start, and
the groups `Support` and `User administration`. None of the made-up users can sign in. The weather feature seeds its
own stations, forecasts and observations through `UseWeatherSeeding`, in the `weather-migrations` run, so after the
spine's seed has created the developer who owns the stations.

It runs through EF's `UseSeeding`/`UseAsyncSeeding` when the migration tool applies migrations, in run mode only: the
AppHost's `AddMigrations` sets `DesignTime.SeedTestDataVariable` on the tool resource, and the published
bundle never seeds. Each part checks for its own rows before inserting, so running it again inserts nothing. Both
delegates are implemented, since the EF CLI calls the synchronous one.

## Data between starts

By default the dev Postgres has no data volume: each `aspire start` and each E2E run gets a fresh, re-seeded database.
`Features:PersistentDatabase` gives the container a persistent lifetime, a data volume and the fixed port 15432 (see
[apphost](apphost.md#persistent-containers)), so the migrations find themselves applied and the seed finds its rows. A migration edited after it was applied, or one removed, is then out of
step with `__EFMigrationsHistory` until `aspire stop --force --volumes` drops container and volume (see
[authentication](authentication.md) for the CLI's version caveat). The E2E fixtures pin the flag off.

## Architecture tests

`FocusTemplate.ArchitectureTests` checks rules every handler or type must follow: `HandlerDbContextTests` reflects over
the API, user management and feature assemblies for the read/write split, `ModelSentinelTests` walks every EF model for
the sentinels, `UserReferenceTests` keeps `UserReference` in step with `User`, and `FeatureTests` keeps the features on
their own projects and the foundations, and the pooled contexts' constructors down to their options.
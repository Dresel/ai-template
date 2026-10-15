# Integration tests

What to follow when changing these files: [the rule](../.claude/rules/integration-tests.md).

An integration test covers one service: DI, middleware, serialization, auth, EF Core and SQL, against a real Postgres
from Testcontainers (`postgis/postgis:17-3.5`, the AppHost's image) and a real Redis (`redis:8.6`, the image of the
AppHost's Aspire version). [Authentication](authentication.md) describes the test scheme that stands in for Keycloak.

## Which project

A test goes where the code under test lives ([architecture](architecture.md#tests)):

- **`SpatialFocus.Foundation.IntegrationTests`**: the spine and the hosting libraries, on the spine's tables and
  user management's routes alone, so the tests hold in any repository that shares the spine. Auditing and the
  read-only context through the spine's pools, the event streams and their handler base, the Keycloak bearer and the
  current user, the API reference's switch and the documents' names.
- **`SpatialFocus.Admin.UserManagement.IntegrationTests`**: users, groups, their stream, permissions, provisioning and
  the administrator bootstrap.
- **`SpatialFocus.Weather.IntegrationTests`**: the feature's endpoints, and auditing and the read-only context through
  its own pools and schema, which take another path through `Data.Foundation` than the spine's.
- **`FocusTemplate.Admin.Api.IntegrationTests`**: what only the product composes. Its own slices (`DemoProfileTests`),
  every endpoint it maps behind authorization, the documents and the reference over them, and the permission rows its
  migrations seed.

The three package projects run on the shared test host, `SpatialFocus.Testing.Host`: the spine, `Api.Hosting`, user
management and the shared features in the order of a product's `Program.cs`, with its own connection names and
`TestHostPermissions`, the permission sets of the packages it includes. A package that gains a permission adds it
there. The product's project runs on the product's `Program`.

## The fixtures

`SpatialFocus.Testing` holds them once. Each host derives an `ApiFixture` of its own, which names its connections and
permissions and creates the schema, and a one-line `ApiTestBase`.

- `PostgresFixture` (assembly fixture, declared in each project's `AssemblyFixtures.cs`) starts the one container,
  creates the `focusdb_reader` login once (it is cluster-level) and hands out databases (`CreateDatabaseAsync`). Only
  `CREATE DATABASE` is serialized, since concurrent creations contend on the template database.
- `ApiFixture<TProgram>` (class fixture, one per test class) creates its own GUID-named database, has the host create
  the schema, grants `focusdb_reader` `SELECT` on every schema (`PostgresFixture.GrantReadAccessAsync`) and points the
  API at it under the host's two database names: the owner, and the read-only one as `focusdb_reader`, so the API's
  queries run on the real role. `Factory.CreateDbContext()` and a feature's `CreateWeatherDbContext()`
  (`WeatherArrangements`) stay on the owner. The schema therefore comes about once per class, in parallel.
- The schema: the product's fixture migrates, the spine's context first and the weather feature's after it, as the
  AppHost orders them. The spine's migrations are a product's (`FocusTemplate.Data`), so the shared test host's fixture
  creates the spine's tables with `EnsureCreated`, adds the permission rows a product's migration seeds, and then each
  package's tables with `CreateTables`, since `EnsureCreated` leaves a database with tables alone.
- `RedisFixture` (assembly fixture) starts one Redis, and each `ApiFixture` connects with a GUID as its `channelPrefix`,
  so a class's streams receive only the signals its own API publishes.
- `ApiTestBase` resets the class database before every test (`ApiFixture.ResetAsync()`,
  [Respawn](https://github.com/jbogard/Respawn), FK-safe). It keeps `__EFMigrationsHistory`, so migrations never re-run,
  and `permissions`, which holds the host's permissions (`Factory.Permissions`: `AdminPermissions.All` for the product,
  `TestHostPermissions.All` for the shared host). Everything else is empty, the seeded `Administrators` group included,
  which `UserManagementArrangements.AddAdministratorsAsync` puts back for the tests that need it.
- The arrangements extend `IApiFixture`, so user management's (`UserManagementArrangements`, in `SpatialFocus.Testing`)
  and a feature's (`WeatherArrangements`, in its test project) work on whichever host runs them.

## Users across tests

`TestAuthenticationHandler.TestUser` is one fixed id, the default of `Factory.CreateAuthenticatedClient()`. The
provisioning middleware remembers each user it has seen for the life of the class, while the reset removes their rows.
So a test whose assertions need the acting user's row (`users`, `user_activities`, a group membership) creates its own
`UserId`. `TestUser` suits tests that do not. A station's owner is a foreign key to `users`, so `AddTestStationAsync`
inserts its owner's row before the station.

## Later

The dev seed is dev and E2E only, and the E2E suite checks it through the production seeding path. If a read-heavy suite
over an expensive shared dataset appears, it gets a seeded, immutable, shared database and fixture of its own (seed
once, read in parallel, never mutate).
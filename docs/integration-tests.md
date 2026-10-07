# Integration tests

What to follow when changing these files: [the rule](../.claude/rules/integration-tests.md).

`FocusTemplate.Admin.Api.IntegrationTests` covers one service: DI, middleware, serialization, auth, EF Core and SQL,
against a real Postgres from Testcontainers (`postgis/postgis:17-3.5`, the AppHost's image).
[Authentication](authentication.md) describes the test scheme that stands in for Keycloak.

## The test database

One container, one database per test class, fresh state per test.

- `PostgresFixture` (assembly fixture) starts the one container, creates the `focusdb_reader` login once (it is
  cluster-level) and hands out databases (`CreateDatabaseAsync`). Only `CREATE DATABASE` is serialized, since
  concurrent creations contend on the template database.
- `ApiFixture` (class fixture, one per test class) creates its own GUID-named database, migrates it, grants
  `focusdb_reader` `SELECT` on it (`PostgresFixture.GrantReadAccessAsync`) and points the API at it under both keys:
  `focusdb` as the owner, `focusdb-readonly` as `focusdb_reader`, so the API's queries run on the real role.
  `Factory.CreateDbContext()` stays on the owner. Migrations therefore run once per class, in parallel.
- `ApiTestBase` resets the class database before every test (`ApiFixture.ResetAsync()`,
  [Respawn](https://github.com/jbogard/Respawn), FK-safe). It keeps `__EFMigrationsHistory`, so migrations never
  re-run, and `permissions`, whose rows the migration seeds from `Permission.All`. Everything else is empty, the seeded
  `Administrators` group included, which `UserManagementArrangements.AddAdministratorsAsync` puts back for the tests
  that need it.

## Users across tests

`ApiFixture.TestUser` is one fixed id, the default of `Factory.CreateAuthenticatedClient()`. The provisioning
middleware remembers each user it has seen for the life of the class, while the reset removes their rows. So a test
whose assertions need the acting user's row (`users`, `user_activities`, a group membership) creates its own `UserId`.
`TestUser` suits tests that do not.

## Later

The dev seed is dev and E2E only, and the E2E suite checks it through the production seeding path. If a read-heavy suite
over an expensive shared dataset appears, it gets a seeded, immutable, shared database and fixture of its own (seed
once, read in parallel, never mutate).
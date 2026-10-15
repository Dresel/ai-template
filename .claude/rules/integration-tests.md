---
paths:
  - "tests/*.IntegrationTests/**"
  - "src/admin/user-management/*.IntegrationTests/**"
  - "src/features/*/*.IntegrationTests/**"
  - "tests/*.Testing/Api/**"
  - "tests/*.Testing.Host/**"
---
# Integration tests

- A test of the spine or the hosting libraries goes into `SpatialFocus.Foundation.IntegrationTests` and uses the
  spine's tables alone, one of user management or a feature into the package's `.IntegrationTests`, both on the shared
  test host. The product's project keeps what only the product composes.
- A test class derives from its host's `ApiTestBase`, which resets the class database before every test: empty apart
  from the migration history and the `permissions` rows. A test arranges exactly the rows it asserts, through
  `Factory.CreateDbContext()`, and never relies on the dev seed.
- Time comes from `Factory.Clock`, a `FakeTimeProvider` the test advances.
- `Factory.CreateAuthenticatedClient()` acts as `TestAuthenticationHandler.TestUser`, `CreateAuthenticatedClient(user)`
  as another user, `Factory.CreateClient()` proves a refusal.
- A test whose assertions need the acting user's rows creates its own `UserId`: the provisioning middleware remembers
  each user for the life of the class, while the reset removes their rows.
- Users, grants and the managed groups are arranged through `UserManagementArrangements` (`GrantAsync`,
  `AddAdministratorsAsync`, `AddManagedGroupAsync`), a feature's rows through its own arrangements on `IApiFixture`.
  A test counts the host's permissions through `Factory.Permissions`.
- A package that gains a permission adds it to `TestHostPermissions`, and a new shared feature goes into the test
  host's `Program.cs` and the schema of its `ApiFixture`.
- Test classes share nothing, so they need no xUnit collection.
- A test of one type alone, without the API host, constructs the type itself and needs no `ApiTestBase`
  (`EventStreamsTests`).

Background: [docs/integration-tests.md](../../docs/integration-tests.md).
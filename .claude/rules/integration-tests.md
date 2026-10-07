---
paths:
  - "tests/*.IntegrationTests/**"
---
# Integration tests

- A test class derives from `ApiTestBase`, which resets the class database before every test: empty apart from the
  migration history and the seeded `permissions`. A test arranges exactly the rows it asserts, through
  `Factory.CreateDbContext()`, and never relies on the dev seed.
- Time comes from `ApiFixture.Clock`, a `FakeTimeProvider` the test advances.
- `Factory.CreateAuthenticatedClient()` acts as `ApiFixture.TestUser`, `CreateAuthenticatedClient(user)` as another
  user, `Factory.CreateClient()` proves a refusal.
- A test whose assertions need the acting user's rows creates its own `UserId`: the provisioning middleware remembers
  each user for the life of the class, while the reset removes their rows.
- Users, grants and the managed groups are arranged through `UserManagementArrangements` (`GrantAsync`,
  `AddAdministratorsAsync`, `AddManagedGroupAsync`).
- Test classes share nothing, so they need no xUnit collection.

Background: [docs/integration-tests.md](../../docs/integration-tests.md).
# Web E2E tests

What to follow when changing these files: [the rule](../.claude/rules/web-e2e.md).

`FocusTemplate.Admin.Web.E2E` boots the whole AppHost through `DistributedApplicationTestingBuilder` and drives it:

- **Aspire system tests** for what crosses resources: BFF and API, ingress, service discovery, startup order,
  telemetry.
- **UI tests** with Playwright through the BFF: DOM, input, caret, focus, keyboard, routing, client validation, user
  flows.

The suite needs Docker, and the developer's AppHost stopped.

## Fixtures

- **`BlazorAppFixture`** (collection `AspireCollection`) starts one AppHost for the collection with every feature flag
  pinned as an argument (no ingress, no analytics, the local Keycloak, nothing persistent, no chaos). It logs the
  developer in once through Keycloak's form and keeps the browser's storage state. `BaseUrl` is the BFF's `http`
  endpoint.
- **`BffPageTest`** is the base class of UI tests: every browser context starts from that storage state, so a test is
  signed in from its first line. A test of the anonymous state opens a fresh context of its own.
- **`IngressAppFixture`** (collection `IngressCollection`, not parallelized) starts a second AppHost with the nginx
  ingress on and tests through `https`.
- **`KeycloakAdmin`** changes the shared realm for a test and puts it back when disposed (see
  [authentication](authentication.md)).
- Tests that only need the AppHost's model (`KeycloakRealmTests`, `PersistentDatabaseTests`) build it without starting
  it.

## UI bugs

Reproduce with the `playwright-cli` skill, with a real browser and keyboard input. For an input or caret bug: navigate,
locate by `data-testid`, set the value, set `selectionStart`/`selectionEnd`, type through key events, then assert the
value and the caret.

## Test ids

The suite selects on `data-testid`, so existing ones stay. An input wrapper names its messages after its own id:
`code-error`, `code-warning`, `code-info`.
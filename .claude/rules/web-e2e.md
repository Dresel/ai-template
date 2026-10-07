---
paths:
  - "tests/*.Admin.Web.E2E/**"
  - "src/admin/*.Admin.Web/**/*.razor"
---
# Web E2E tests

- Tests select elements by `data-testid`. Keep every existing one. An input wrapper's messages carry its id plus
  `-error`, `-warning` or `-info`.
- Reproduce a UI bug with the `playwright-cli` skill and real keyboard input. An input or caret bug asserts the value
  and the caret.
- Every browser context starts from the session the fixture captured. A test of the anonymous state starts from a fresh
  context.
- The fixtures pin the feature flags as arguments, and a new flag gets pinned there too.
- Realm changes go through `KeycloakAdmin`, which puts the realm back when disposed.
- The E2E suite boots an AppHost of its own, so the developer's AppHost must be stopped.

Background: [docs/web-e2e.md](../../docs/web-e2e.md).
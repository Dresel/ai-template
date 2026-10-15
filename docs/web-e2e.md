# Web E2E tests

What to follow when changing these files: [the rule](../.claude/rules/web-e2e.md).

`FocusTemplate.Admin.Web.E2E` boots the whole AppHost through `DistributedApplicationTestingBuilder` and drives it:

- **Aspire system tests** for what crosses resources: BFF and API, ingress, service discovery, startup order,
  telemetry.
- **UI tests** with Playwright through the BFF: DOM, input, caret, focus, keyboard, routing, client validation, user
  flows.

The suite needs Docker, and the developer's AppHost stopped.

## Fixtures

The bases live in `SpatialFocus.Testing` (`E2E/`), and the product derives the fixtures that name its AppHost.

- **`BlazorAppFixture`** starts one AppHost for the collection and logs the developer in once through Keycloak's form,
  keeping the browser's storage state. `BaseUrl` is the BFF's `http` endpoint. The product's **`AdminAppFixture`**
  (collection `AspireCollection`) creates the builder for `FocusTemplate.AppHost` with every feature flag pinned as an
  argument (no ingress, no analytics, the local Keycloak, nothing persistent, no chaos) and names the BFF, the
  Keycloak resource and the realm.
- **`BffPageTest`** is the base class of UI tests: every browser context starts from that storage state, so a test is
  signed in from its first line. A test of the anonymous state opens a fresh context of its own.
- **`IngressAppFixture`** starts a second AppHost with the nginx ingress on and tests through `https`. The product's
  **`AdminIngressAppFixture`** (collection `IngressCollection`, not parallelized) pins the flags with the ingress on.
- **`KeycloakAdmin`** changes the shared realm for a test and puts it back when disposed (see
  [authentication](authentication.md)).
- The collection definitions stay in the product's suite, since xUnit finds them only in the assembly that runs the
  tests.
- Tests that only need the AppHost's model (`KeycloakRealmTests`, `PersistentDatabaseTests`,
  `MigrationResourceTests`) build it without starting it.

## A feature's tests

A feature cannot boot an AppHost of its own, so it defines its UI tests and each product runs them
([architecture](architecture.md)). `SpatialFocus.Weather.E2E` is a library on `xunit.v3.extensibility.core` and
`xunit.v3.assert` with abstract classes on `BffPageTest` (`WeatherTests`, `StationsTests`), referencing
`SpatialFocus.Testing` alone. The product's suite references it and runs each class through a one-line subclass in its
collection:

```csharp
[Collection(AspireCollection.Name)]
public sealed class AdminWeatherTests(AdminAppFixture app) : WeatherTests(app);
```

xUnit skips the abstract class and runs its facts as the subclass's, against the product's AppHost and session. A
product without the feature leaves out the subclasses and the reference.

## UI bugs

Reproduce with the `playwright-cli` skill, with a real browser and keyboard input. For an input or caret bug: navigate,
locate by `data-testid`, set the value, set `selectionStart`/`selectionEnd`, type through key events, then assert the
value and the caret.

## Test ids

The suite selects on `data-testid`, so existing ones stay. An input wrapper names its messages after its own id:
`code-error`, `code-warning`, `code-info`.
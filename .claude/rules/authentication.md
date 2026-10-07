---
paths:
  - "src/admin/*.Admin.Web.Bff/**"
  - "src/admin/*.Admin.Api/Authentication/**"
  - "src/admin/*.Admin.Web/{App.razor,_Imports.razor}"
  - "src/admin/*.Admin.Web/Infrastructure/Authentication/**"
  - "src/admin/*.Admin.Web/Foundation/Shell/MainLayout.razor"
  - "src/admin/*.Admin.Web.ClientServiceDefaults/{CsrfHeaderHandler,RedirectToLoginHandler,ServiceCollectionExtensions}.cs"
  - "src/admin/*.Admin.Shared/{BffClaimTypes,UserClaim,UserInfoResponse}.cs"
  - "src/*.AppHost/{AuthenticationExtensions.cs,keycloak/**}"
  - "tests/*.Admin.Web.E2E/{AuthenticationTests,BlazorAppFixture,KeycloakAdmin}.cs"
  - "tests/*.IntegrationTests/{AuthenticationTests,TestAuthenticationHandler}.cs"
---
# Authentication

- Every API endpoint requires a signed-in user through the spec's `@useAuth`. The API has no fallback policy, which would
  lock the health probes. `EveryApiEndpointRequiresAuthorization` checks the spec.
- A new proxied API gets its own route entry with the `ProxiedApi` policy in the BFF's `appsettings.json`.
- Proxied clients are registered with `AddProxiedHttpClient`, which adds `X-CSRF` and sends a 401 through login.
- A 401 from the BFF means the session is gone. A 502 on a proxied call or on `/bff/user` means the BFF and the API
  disagree about tokens, or the API is down.
- Return URLs of `/bff/login` and `/bff/logout` are local paths only.
- The API keeps `MapInboundClaims = false`, so claims keep Keycloak's names.
- A handler reads the user from `ICurrentUser.Id`. Code that runs without a person reads `IdOrDefault`.
- `keycloak/focus-realm.json` and `AddLocalKeycloakRealm` change together.
- Every page but Home requires a signed-in user. A signed-in user without access sees `Forbidden`, and only an anonymous one
  is sent to login.
- Keep the test ids `user-name`, `login-button`, `logout-button`, `authorizing`, `forbidden`.
- Integration tests authenticate through `TestAuthenticationHandler` (`Authorization: Test <UserId>`). E2E tests start
  from the fixture's captured login, and `KeycloakAdmin` puts the realm back when disposed.

Background: [docs/authentication.md](../../docs/authentication.md).
# Authentication

What to follow when changing these files: [the rule](../.claude/rules/authentication.md).

Keycloak is the identity provider and the BFF holds the session: the shape of Duende's BFF without Duende's BFF, which
is paid in production. Authentication is always on, and `Features:LocalKeycloak` only decides whether the AppHost runs a
Keycloak or takes an existing realm.

Packages: `Yarp.ReverseProxy` with `Microsoft.Extensions.ServiceDiscovery.Yarp`,
`Microsoft.AspNetCore.Authentication.OpenIdConnect` (cookie and code flow), `Duende.AccessTokenManagement.OpenIdConnect`
(refreshes the user's access token for the proxied calls) and `Microsoft.AspNetCore.Authentication.JwtBearer` in the
API. The token management is Apache-2.0, unlike Duende's BFF and IdentityServer.

## AppHost

- **Local realm** (`Features:LocalKeycloak` on, the default, run mode only): `AddLocalKeycloakRealm` adds
  `AddKeycloak("keycloak", 8080)` with `keycloak/focus-realm.json`: realm `focus`, the confidential client `admin-bff`
  with an audience mapper that stamps `admin-api` into the access token, and the login `developer` / `developer`, whose
  user id is `WellKnownUsers.Developer`. Sessions last 7 days idle and 30 days at most, and survive browser restarts
  when "Remember me" is ticked. `WithKeycloakClient` hands the BFF that login as `Oidc__LoginHint`, sent as
  `login_hint` inside the pushed request, so the form asks only for the password. The realm file and the values in
  `AddLocalKeycloakRealm` change together by hand.
- **Keycloak's endpoint** is named `http` whatever its scheme: the integration switches it to https at start when the
  dev certificate is available, and a separate `https` endpoint would yield a `tcp://` authority. Both handlers keep the
  default https-metadata requirement, so a Keycloak left on http fails at the first login with the handler's own
  message.
- **External realm** (the flag off, and always when publishing, since the imported realm's login and client secret are
  public): `AddExternalKeycloakRealm` takes a realm on any Keycloak from the parameters `oidc-authority` (the realm's
  URL, `https://sso.example.com/realms/focus`), `oidc-admin-bff-secret`, `oidc-admin-bff-client-id` and
  `oidc-admin-api-audience` (defaults `admin-bff`, `admin-api`). That realm needs the confidential client, the audience
  mapper, `sub` as the user's UUID, and the redirect URIs `http://localhost:5770/signin-oidc` and
  `https://localhost:7770/signin-oidc` with their `/signout-callback-oidc` counterparts. The ingress's port is pinned
  for this. The AppHost models it as the external service `keycloak`, whose health check probes the realm URL itself,
  since a health-check path would replace the URL's last segment, the realm. No login hint goes to an external realm.
- `KeycloakRealm` carries what the projects need either way. `WithKeycloakAudience` and `WithKeycloakClient`
  (`AuthenticationExtensions`) hand a project `Oidc__Authority` plus its audience or its client id and secret, and wait
  for the Keycloak.
- **Persistent Keycloak** (`Features:PersistentLocalKeycloak`, off by default): a persistent container with a data
  volume, kept running between starts, so no start waits for the JVM and the realm import, and the BFF's session
  cookie signs in again without a login. Keycloak then skips importing a realm that exists: a change to
  `focus-realm.json` arrives only after `aspire stop --force --volumes` (before Aspire.Hosting 13.6 the CLI warns it
  cannot verify volume ownership and still tries, with `docker volume rm` of the volume named after the resource as the
  fallback). The E2E fixtures pin it off, since `KeycloakAdmin` edits the realm.

## BFF

- **Session**: cookie `__Host-focus.session` (HttpOnly, Secure, SameSite=Lax) plus the OIDC code flow with the
  authorization request pushed (PAR, the handler's default against Keycloak's advertised endpoint) and the code posted
  back (form_post, also the default), which keeps the code out of URLs. The E2E login test asserts both. Tokens live in
  the cookie and are refreshed by the token management, a minute before expiry.
- **Endpoints**: `/bff/login?returnUrl=` and `/bff/logout?sid=&returnUrl=` (a GET, so the browser can carry on to
  Keycloak's end-session page, with the session id as its CSRF token) accept local return paths only. `/bff/user` answers
  401 when anonymous, otherwise `UserInfoResponse`: the user's claims, the BFF's own `bff:logout_url` (`BffClaimTypes`
  in `Admin.Shared`) and the permissions it asks the API for (`GET /users/me`), with `Cache-Control: no-store`.
- **Proxied routes**: the `/_api` route is declared in full in the BFF's `appsettings.json` under its Aspire-generated
  name (`route-admin-api`), carrying the `ProxiedApi` policy, while the AppHost's environment adds the destination and
  the prefix transform. The policy requires a session and an `X-CSRF` header (the client sends `1`, any value passes), else
  401 or 403, since the cookie handler's redirects are turned into status codes. A further proxied API needs an entry
  of its own. A forgotten one fails closed, because the transform sends no token to a route without the policy and
  strips any `Authorization` header the caller sent.
- **Tokens**: a request transform on those routes (`AddAccessTokenTransform`) puts the user's access token on the
  outgoing request. It and `/bff/user` get the token through `GetAccessTokenOrSignOutAsync`: a refresh Keycloak refuses
  means its session is gone, so the cookie session is signed out, the transform answers 401 without forwarding, and
  `/bff/user` answers 401 on the same load, so the client shows the user signed out.
- **401 and 502**: a 401 the API itself answers on a proxied route reaches the browser as 502, without the API's
  `WWW-Authenticate`: the BFF sent a token of a live session, and a new login would only bring the same kind of token.
  So a 401 from the BFF always means the session is gone, and a 502 means the BFF and the API disagree about tokens
  (audience, issuer) or the API cannot be reached, and the reason is in the API's log. `/bff/user` turns an
  `HttpRequestException` from its call to the API into 502, while a timeout or an open circuit of the resilience handler
  currently ends in a 500 (backlog §3.2).
- **Cookies**: Lax plus header, because Strict withholds the cookie on the redirect back from the cross-site identity
  provider, so the first page after login is anonymous and loops. The header is the CSRF defense: a cross-site page
  cannot add it without a CORS preflight the BFF never grants. The session cookie is Secure whatever scheme reaches the
  BFF (`SecurePolicy.Always`, since behind the ingress that scheme is http) and its `__Host-` prefix makes the browser
  insist on `Path=/` and no `Domain`, so no sibling subdomain can plant a session. The correlation and nonce cookies
  keep their defaults, SameSite=None and Secure, which the cross-site form post needs. Chrome and Firefox accept Secure
  cookies from `http://localhost`, Safari does not.

## API

JwtBearer with `Oidc:Authority` and `Oidc:Audience`, and `MapInboundClaims = false`, so claims keep Keycloak's names.
Every generated endpoint requires authorization because `api.tsp` declares `@useAuth(BearerAuth)`. A fallback policy
would also lock the health probes. `AuthenticationTests.EveryApiEndpointRequiresAuthorization` catches a spec that lost
it.

`ICurrentUser` is `HttpContextCurrentUser`: the `sub` claim is Keycloak's user UUID and therefore the `UserId`, with no
directory lookup (a `sub` that is no UUID fails validation). It is a singleton over `IHttpContextAccessor`, because its
consumer, the auditing interceptor, is one. `IdOrDefault` is null without a signed-in user and serves auditing and
authorization. A handler reads `Id`, which throws then. The provisioning middleware reads the claims itself
(`ClaimsPrincipal.UserIdOrDefault`).

## WASM client

- `BffAuthenticationStateProvider` asks `/bff/user` once per load. Only its 401 means anonymous, and any other failure
  throws into the error UI, since a login would bring back the same state.
- `[Authorize]` in `_Imports.razor` and `AuthorizeRouteView` in `App.razor` guard every page but `Home`
  (`[AllowAnonymous]`). A page the user may not see renders `Forbidden` for a signed-in user and `RedirectToLogin` for an
  anonymous one, since a signed-in user sent to log in would come straight back from Keycloak's session.
- Login and `RedirectToLogin` do a full load of `bff/login`, logout navigates to the `bff:logout_url` claim with
  `forceLoad`: both live outside the client router.
- `AddProxiedHttpClient` adds `CsrfHeaderHandler` and `RedirectToLoginHandler`, which turns a 401 into a full load of
  `bff/login` with the current page as return URL. A 502 stays an error, since logging in again would only loop.
- Test ids: `user-name`, `login-button`, `logout-button`, `authorizing`, `forbidden`.

## Tests

- **Integration**: `TestAuthenticationHandler` is the default scheme: `Authorization: Test <UserId>` is that user,
  anything else is anonymous. The `Oidc:*` settings the fixture sets only satisfy validation.
- **E2E**: `BlazorAppFixture` logs the developer in once through Keycloak's form (`#username`, `#password`,
  `#kc-login`) and hands the storage state to every context. `AuthenticationTests` start from a fresh context for the
  anonymous home page, login (including the prefilled username, PAR and form_post), logout, the `/_api` gate, the
  cookie's `Secure` and `__Host-`, `/bff/user`'s `no-store`, a session Keycloak ends behind the BFF's back, and an API
  refusing a live session's token.
- **`KeycloakAdmin`** works on Keycloak's admin REST API (signed in through `BlazorAppFixture.SignInToKeycloakAsync`):
  it shortens the access token lifespan below the renewal window, deletes a session by its `sid`, removes the
  `admin-api-audience` mapper, creates a login without permissions for `UserManagementTests`, and puts the realm back
  when disposed, since the whole collection shares one Keycloak.
- The fixtures pin `Features:LocalKeycloak=true` and `Features:PersistentLocalKeycloak=false`. `KeycloakRealmTests`
  covers both flags on the AppHost's model alone, built and never started.

Open items (server-side sessions, backchannel logout, revocation, the Data Protection key ring, PKCE for the Public API)
are in the backlog, §3.2.
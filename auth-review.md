# Auth review: open and fixed tickets

This comes from the max-effort review of the staged Keycloak/BFF change on 2026-09-24. It found 15 issues: 5 are fixed,
1 you resolved yourself, and 9 are still open. One of the open ones, K1, was in progress when you left. My fixes are
unstaged working-tree edits, and nothing is staged or committed.

**Last test runs:**
- The E2E suite passed 6/6 after the fifth fix.
- The Admin API integration tests passed 16/16 after the fourth fix. The fifth fix only changed the BFF.
- I have not built your later edits: the trailing comma in `LocalPathOrRoot` and the TODO in `ServiceCollectionExtensions`.

**Path shorthand:** `Web.Bff/…` means `src/admin/FocusTemplate.Admin.Web.Bff/…`, and the same pattern applies to `Api/…`,
`Web/…` and `Web.ClientServiceDefaults/…`. `AppHost/…` means `src/FocusTemplate.AppHost/…`.

## Fixed

| Finding | Fix | Where |
|---|---|---|
| **The session cookie went to every proxied backend.** The browser's `Cookie` header, including the `focus.session` ticket with all tokens, reached the API, the dashboard and Umami. Their `Set-Cookie` headers also came back onto the BFF origin. | `AddCookieIsolationTransform`: every route drops the `Cookie` header. Responses drop the destination's `Set-Cookie` values but keep the BFF's own renewed session cookie. | `Web.Bff/ReverseProxyBuilderExtensions.cs:15`, `Web.Bff/Program.cs` |
| **The policy name check was case-sensitive.** YARP and ASP.NET Core resolve policy names case-insensitively, so a route declared with `"proxiedApi"` was protected but never got a token. | `StringComparison.OrdinalIgnoreCase` | `Web.Bff/ReverseProxyBuilderExtensions.cs:41` |
| **A `~/` returnUrl passed `IsLocalUrl`.** The OIDC handlers redirect it verbatim, so it lands on `/~/…`. | Only paths that start with `/` and also pass `IsLocalUrl` are accepted. | `Web.Bff/AuthenticationEndpoints.cs:51` |
| **A failed token refresh left the 14-day session alive.** The UI showed the user as signed in while every API call answered 401, until a manual logout. | The transform signs the cookie session out and answers 401 without forwarding. `RedirectToLoginHandler` turns any proxied 401 into a full load of `bff/login?returnUrl=<page>`. | `Web.Bff/ReverseProxyBuilderExtensions.cs:60`, `Web.ClientServiceDefaults/RedirectToLoginHandler.cs` (new), `ServiceCollectionExtensions.cs` |
| **A client's own bearer token could reach the internal API.** YARP copies the caller's `Authorization` header. | Routes without the policy strip `Authorization`. Policy routes either overwrite it or forward nothing. | `Web.Bff/ReverseProxyBuilderExtensions.cs:44` |

The BFF and WASM bullets in AGENTS.md are updated for these fixes.

**Resolved by you:** H2, `maui.md` staged by accident. It is untracked again.

## In progress: K1, http authority and publish mode

**The finding (confirmed):** when `Oidc__Authority` resolves to http, every BFF and Admin API request fails with a 500, the
health endpoints included. It is not just the first login, as AGENTS.md says.
- **When it happens:** in run mode without a usable dev certificate, and in publish mode.
- **Why:** the PostConfigure of the OIDC and JwtBearer handlers throws "must use HTTPS" on every request.
- **Publish mode also has no realm,** although AGENTS.md says the realm is baked in with `WithDockerfile`.

**Decided:** prod uses an external OIDC provider. There will be no fail-fast https validator on `OidcOptions`, since you
declined it.

**To do (nothing is written yet):**
1. **AppHost.**
   - Add Keycloak and the realm import in run mode only.
   - In publish mode there is no Keycloak, and admin-api and admin-bff get `Oidc__Authority` from an `oidc-authority`
     parameter. This is registry's `WithLocalOIDC`/`WithRemoteOIDC` split.
   - Make the client id and the audience parameters, with the realm's values as defaults, next to the existing
     `oidc-admin-bff-secret`. Registry does the same with `AddParameter("oidc-client-id", "registry-bff")`.
   - Check that the default of an `AddParameter(name, value)` stays unpublished (`publishValueAsDefault: false`), so the
     environment has to supply it. The `external-parameters` doc does not say.
   - Consider `builder.AddExternalService("oidc", authorityParameter)`, which would show the provider as a resource.
   - While in there, rename `AuthenticationExtensions`: AGENTS.md's naming rule wants `ProjectResourceBuilderExtensions`.
2. **AGENTS.md, Authentication → AppHost bullet.**
   - Publish uses an external provider, and the environment supplies the authority, client id, secret and audience.
   - The realm file is dev-only.
   - Replace "fails at the first login" with "every BFF and API request fails with 500".
3. **AGENTS.md, Running.** Add a trusted dev certificate as a prerequisite. From aspire.dev (certificate-configuration):
   - `aspire run` in an interactive session creates and trusts the certificate. Check that `aspire start` does the same.
   - A non-interactive run on Windows only generates it, untrusted.
   - `dotnet test` (the E2E suite, via `DistributedApplicationTestingBuilder`) never goes through the CLI. So run
     `aspire certs trust` (or `dotnet dev-certs https --trust`) once.
   - To troubleshoot, run `aspire certs clean`, then `aspire certs trust`.
4. **Verify.**
   - Build the AppHost and the E2E project, then run the E2E suite: run mode must be unaffected.
   - Run `aspire publish` into a scratch folder and check the output: no `keycloak` resource, and the `oidc-*` parameters
     are present.

## Open

Ordered by severity.

### O1: the OIDC cookie override is inert, and AGENTS.md explains it wrongly (confirmed)

**Where:** `Web.Bff/WebApplicationBuilderExtensions.cs:50-53`, and the AGENTS.md BFF bullet from "Lax plus header, not
Strict" to its end.

**Why it is inert:** both cookies default to `SecurePolicy = Always`, and the override only changes `SameSite`.
- Chromium and Firefox accept Secure cookies on `http://localhost`.
- Behind the ingress, the BFF sees https (`ASPNETCORE_FORWARDEDHEADERS_ENABLED`).
- Safari on `http://localhost` fails with "Correlation failed" today either way.

**Your question: can we harden the cookie settings?** Yes, and it is mostly deletion.
- **Delete lines 50-53:** `ResponseMode.Query` and the two `SameSite.Lax` lines. That restores the handler defaults:
  `form_post`, with correlation and nonce cookies set to `SameSite=None; Secure`. The authorization code then travels in
  a POST body instead of the URL, so it stays out of browser history, logs and the Referer header.
- **Make the session cookie `focus.session` `SameSite = Strict`.** Line 25 only restates the cookie handler's default,
  Lax.
  - **Why Strict works here:** the first request after the callback's redirect is the static WASM page. `/bff/user` and
    `/_api` are same-origin fetches, which carry a Strict cookie.
  - `X-CSRF` stays the actual CSRF defense.
  - **Optional on top:** `Cookie.SecurePolicy = Always` and a `__Host-` name prefix. Check both on
    `http://localhost:5770` first.
- **Rewrite the AGENTS.md paragraph** with the real reasoning, then re-run the E2E login, logout and ingress tests.
- **Only if plain-http hosts other than localhost must work:** keep Query and Lax, and add `SecurePolicy = SameAsRequest`
  to both cookies instead.

### Z1: per-slice auth hooks fail open (confirmed, security)

**Where:** `Api/Features/WeatherForecasts/WeatherForecastsEndpoints.Hooks.cs:5`

**Problem:** authorization is opt-in through the partial `ConfigureGroup` hook.
- A new slice without a hooks file compiles and serves anonymously.
- Its writes are audited as `WellKnownUsers.System`.
- Only `EveryApiEndpointRequiresAuthorization` catches it.

**The AGENTS.md reason against a fallback policy does not hold.** It claims a fallback would lock the health probes. But
the AppHost registers no health check for admin-api, and `/health` and `/alive` can be marked anonymous.

**Fix:**
- Add a `FallbackPolicy` that requires an authenticated user, in `AddApiAuthentication`.
- Add `AllowAnonymous` on the health endpoints in ServiceDefaults `MapDefaultEndpoints`, and check that `openapi.yaml`
  still loads.
- Keep the hooks for extras only.
- Fix the AGENTS.md sentence.

### W1: forbidden signed-in users loop through login (confirmed, latent)

**Where:** `Web/App.razor:6`

**Problem:** `NotAuthorized` renders `RedirectToLogin` for everyone, including signed-in users who fail a requirement.
- The first page with `[Authorize(Roles = …)]` would loop: page → `/bff/login` → Keycloak SSO → page.
- Keycloak's roles arrive nested in `realm_access`, not as `ClaimTypes.Role`. So every user would fail role checks until
  the roles are mapped.

**Fix:** redirect only when `context.User.Identity?.IsAuthenticated != true`, and otherwise show an access-denied
message, like the standalone WASM template. Map the roles when the first role check appears.

### S-1: a stale or replayed login callback ends in a 500 (plausible)

**Where:** `Web.Bff/WebApplicationBuilderExtensions.cs:41`

**Problem:** there is no `OnRemoteFailure`. `AuthenticationFailureException` becomes a 500 when:
- the correlation cookie has expired (`RemoteAuthenticationTimeout` is 15 minutes, Keycloak's login form lasts 30)
- the Back button replays `/signin-oidc`
- Keycloak answers with `error=…`

**Fix:** handle `OnRemoteFailure` and call `HandleResponse()`. Redirect to a page with a message and a retry link rather
than straight back to `/bff/login`, so that a persistent failure cannot loop. A wrong client secret or `access_denied`
would be such a failure.

### S-2: Keycloak restarts orphan surviving BFF sessions (plausible)

**Where:** `AppHost/AppHost.cs:21`

**Problem:** every AppHost start recreates Keycloak with new signing keys and no sessions. The BFF cookie survives,
because its Data Protection keys persist in `%LOCALAPPDATA%`.
- **Covered by the refresh fix:** the API's 401 for the unknown signing key now sends the client through login, via
  `RedirectToLoginHandler`.
- **Still open:** logging out of such a session sends the old `id_token_hint`, and Keycloak shows "Invalid parameter:
  id_token_hint".

**Options:**
- `WithDataVolume()` on Keycloak in run mode. The trade-off: the realm import is skipped once the realm exists, so a
  change to `focus-realm.json` needs the volume deleted.
- Pin the key providers in `focus-realm.json`.

### L2: no shared Data Protection keys for BFF sessions (plausible, not triggered)

**Where:** `Web.Bff/WebApplicationBuilderExtensions.cs:22`

**Problem:** the session, the nonce and the OIDC state all need the key ring. Nothing calls `AddDataProtection`,
`PersistKeysTo…` or `SetApplicationName`.
- With two replicas and no sticky routing, a callback that lands on the other replica fails with "Unable to unprotect
  the message.State".
- A recreated container signs everyone out.

**Fix:** when the BFF gets deployed or scaled out, add `AddDataProtection().SetApplicationName(…).PersistKeysTo…` with
Redis, blob storage or the database. This fits the "server-side sessions wait for Redis" item.

### S-3/4: the new security guards have no failing test (plausible, test coverage)

**Where:** `Web.Bff/AuthenticationEndpoints.cs:29`

**Problem:** weakening any of these guards leaves the suite green:
- the logout `sid` check
- the returnUrl filter, which guards against open redirects
- the route filter in the token transform, without which the token reaches the dashboard and Umami
- `MapInboundClaims = false` in the API. Without it, `sub` gets renamed and `HttpContextCurrentUser` throws on every
  authenticated `SaveChanges`.
- this round's fixes: cookie isolation, stripping `Authorization`, and signing out on a failed refresh

There is no BFF integration test project, although AGENTS.md puts auth and middleware behavior at the integration level.

**Fix:**
- Add `FocusTemplate.Admin.Web.Bff.IntegrationTests`, using `WebApplicationFactory`, a test scheme and a stub destination.
- In the API, add one test with a locally signed JWT, so that `sub` provably survives.

### K2: the dev realm accepts any redirect and post-logout URI (confirmed, low)

**Where:** `AppHost/keycloak/focus-realm.json:15`

**Problem:** the realm allows `redirectUris: ["*"]` and `"+"` for post-logout, and the client secret is committed.
- The dev Keycloak is an open redirector.
- It lets wrong callback URLs through, such as the port-less one from nginx's `$host`.

Once K1 lands, the realm is dev-only by design, which lowers the severity further.

**Fix:** list the exact `/signin-oidc` and `/signout-callback-oidc` URIs for `http://localhost:5770` and the ingress
origin. First check whether the E2E run uses random ports.

## Loose ends

- **`ValidAudiences = [audience]` vs `jwt.Audience = audience`:** equivalent for one audience. JwtBearer copies `Audience`
  into `ValidAudience` when that is empty. `ValidAudiences` only matters for several audiences.
- **`// TODO: ErrorBoundar & MessageCenter`** in `Web.ClientServiceDefaults/ServiceCollectionExtensions.cs:27`: a
  session-expired notice instead of the redirect. The handler would call a singleton notifier, and a layout component or
  ErrorBoundary would show the notice.
  - Removing the `RedirectToLoginHandler` line means also deleting `RedirectToLoginHandler.cs`, the
    `Microsoft.AspNetCore.Components` using, and the sentence in AGENTS.md's WASM bullet.
  - Without the handler, a failed refresh no longer brings the user back to login on its own.

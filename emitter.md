# Emitter: open work in typespec-http-csharp-slim

*Written 2026-10-08. Hand-over for a session in the emitter repository. The bump into this repository follows
**Bumping the emitter** in `docs/spec-first.md`. The smaller emitter follow-ups stay in backlog §9.*

## 1. C# names ignore `@friendlyName` (first)

**Found:** TCGC's `getLibraryName` (`@azure-tools/typespec-client-generator-core`, `dist/src/public-utils.js`) names a
type by its `@clientName` for the scope, else its `@friendlyName`, else its declared name, and the emitter writes that
name into C#. With `@friendlyName("Primitives.GroupId")` in `src/spec/primitives.tsp`, the regeneration changed 25 files:

```diff
-using FocusTemplate.Primitives;
-public sealed partial record GroupsGetQuery(GroupId Id) : IQuery<GroupsGetResult>;
+public sealed partial record GroupsGetQuery(Primitives.GroupId Id) : IQuery<GroupsGetResult>;
```

It compiles only because the generated code lives under `FocusTemplate.*`, where `Primitives` resolves to
`FocusTemplate.Primitives`. `Http.ValidationProblemDetails` (task 2) would not compile: the class lives in
`FocusTemplate.Admin.Shared`, and there is no `FocusTemplate.Http`.

**Fix:** C# declarations and references take the declared name (`type.name`, through the SDK type's raw type).
`@friendlyName` changes only the OpenAPI schema name. An implicit `@clientName` set by `@typedId` would cover the ids
but not the library's models.

**Done when:** a test with a scalar and a model that carry a dotted `@friendlyName` generates the same C# as without
one, and here the regeneration after the bump changes no C# file.

## 2. `Http.` friendly names on the library's models (after 1)

In `lib/main.tsp`, namespace `SpatialFocus.Http`:

| Type | Today | After |
|---|---|---|
| `ProblemDetails` | `@friendlyName("ProblemDetails")` | `@friendlyName("Http.ProblemDetails")` |
| `ValidationProblemDetails` | none, published as `SpatialFocus.Http.ValidationProblemDetails` | `@friendlyName("Http.ValidationProblemDetails")` |
| `Violation` | none, published as `SpatialFocus.Http.Violation` | `@friendlyName("Http.Violation")` |
| `ViolationSeverity` | none, published as `SpatialFocus.Http.ViolationSeverity` | `@friendlyName("Http.ViolationSeverity")` |

The rule behind it: a schema of the service namespace stays bare, every other one carries the last segment of its
namespace (`UserManagement.GroupResponse`, `Primitives.UserId`, `Http.ProblemDetails`). `Http.` says what the types are,
the error format of RFC 9457, and keeps them apart from `Primitives.` (`Core.` was the alternative).

## 3. In this repository after the bump

- **Primitives:** tried on 2026-10-08 and reverted. After the bump, the seven `@friendlyName("<Name>")` in
  `src/spec/primitives.tsp` become `@friendlyName("Primitives.<Name>")`, and the comment above them (lines 11–12)
  says the schema takes the last namespace segment as prefix, like a slice's `UserManagement.<Name>`.
- **Before `npm run gen`:** `npm ci` whenever the tgz under `.npm/` changed since the last install. A `node_modules`
  from 2026-10-01 regenerated the user management under the old `UserManagementGroups…` names. Optional guard:
  `scripts/gen.mjs` compares the installed emitter with the integrity in `package-lock.json` and stops on a mismatch.
- **Expected diff:** the spec and `openapi.yaml` (`Primitives.*`, `Http.*` and their references), no C# file.
- **Docs:** the naming rule goes into `docs/spec-first.md`, and `.claude/rules/spec-first.md` gets "a typed id carries
  `@friendlyName("Primitives.<Name>")`".

## 4. QUERY in the OpenAPI document (when upstream lands)

`users.tsp` declares the search `@httpQuery` and `@post`, since TypeSpec's `HttpVerb` has no QUERY (still in 1.17). The
emitter generates QUERY on both ends (`MapMethods(…, [HttpMethods.Query])`, `HttpMethod.Query`), the stock `openapi3`
writes `post:`. Checked 2026-10-08: `POST /users` answers 405, `QUERY /users` 401, and Scalar's "Try it" sends POST.

Upstream: [microsoft/typespec#11171](https://github.com/microsoft/typespec/issues/11171), the QUERY method (RFC 10008)
with OpenAPI 3.2 `query` operations, open, milestone October 2026. Once it ships, the spec uses the native verb,
`@httpQuery` leaves the library and the emitter, and `docs/spec-first.md` (**Spec style**) and its rule follow. No
interim patch: moving `post:` to `query:` in `scripts/gen.mjs` would work, but Scalar's support for `query:` is untested.

## 5. Later: server-sent events

Only when backlog §6 **Live updates via SSE from the spec** is promoted. The events are signals (what changed, by
typed id), so there are no event ids, no `Last-Event-ID` and no `retry:`. A spec, sketched before the stations slice
exists:

```typespec
import "@typespec/sse";
import "@typespec/events";
using TypeSpec.SSE;
using TypeSpec.Events;

/** A station changed. Read it again. */
model StationChanged { id: StationId; }

/** An alert was raised or resolved. */
model AlertChanged { id: AlertId; stationId: StationId; }

@events
union AdminEvents {
  stationChanged: StationChanged,
  alertChanged: AlertChanged,
}

enum EventTopic { stations, alerts }

@route("/events")
interface Events {
  /** Change signals for what the caller may see. The stream ends when the access token expires: reconnect and reload. */
  @requiresPermission(Permissions.Stations.ViewStations)
  @get
  subscribe(@query(#{ explode: true }) topics?: EventTopic[]): SSEStream<AdminEvents>;
}
```

What the emitter generates:

- **Server:** a stream request per operation (`EventsSubscribeQuery : IStreamQuery<AdminEvents>`, Mediator 3.1), whose
  handler answers with an `IAsyncEnumerable`. The endpoint passes it through `TypedResults.ServerSentEvents`, the
  variant name as `event:`, with a keep-alive comment every 15–30 s (below nginx's 60 s `proxy_read_timeout` and
  YARP's 100 s activity timeout), `X-Accel-Buffering: no` and no response compression.
- **Contracts:** the event models as records in `Shared`, which the publishers (handlers, `observer`) use as well, and
  the union as a C# union like the result types.
- **Client:** a method reading `SseParser` into an `IAsyncEnumerable<AdminEvents>`, with browser response streaming
  on, which skips event names it does not know. Reconnecting stays with the client service that holds the tab's one
  stream.

## 6. Two sources of 403 on one operation (not decided)

Comes with resource-based authorization, which is not in the backlog yet. An operation then answers 403 from two places:
the authorization middleware, when the caller lacks the `@requiresPermission` permission, and the handler, after
checking the caller's relations to the resource (`Problem<403>` in the spec, a `Forbidden` case in its result). The
generated client maps a modeled status by the status alone, so a middleware 403 would become the `ForbiddenProblem` as
well, with or without a problem body.

- **A, a problem `type` for the missing permission (suggested):** an `IAuthorizationMiddlewareResultHandler` answers a
  failed `PermissionRequirement` with a problem of its own `type` (`…/missing-permission`), the permission named in an
  extension. The type URI is a constant in `SpatialFocus.Http`. The generated client maps a 403 of that type to
  `ApiFailure` and every other 403 to `ForbiddenProblem`, and the client's permission refresh (next to
  `RedirectToLoginHandler`) reacts to that type only. RFC 9457 has `type` for this, and a resource 403 never reloads
  `/bff/user`. The handler's 403 keeps the default problem for its status.
- **B, map a matching problem body only:** the client maps a 403 to `ForbiddenProblem` only when its body is the
  modeled problem, anything else to `ApiFailure`. It relies on the middleware 403 having no body today, and turns wrong
  without a failing test once error responses get problem bodies.

**Done when:** an API integration test shows a permission 403 with the type and the permission, and the generated
client of an operation with `Problem<403>` sorts the two 403s.

# Emitter: open work in typespec-http-csharp-slim

*Written 2026-10-08, sections 1–3 updated 2026-10-09. Hand-over for a session in the emitter repository. The bump
into this repository follows **Bumping the emitter** in `docs/spec-first.md`. The smaller emitter follow-ups stay in
backlog §9.*

## 1. C# names ignore `@friendlyName` (done)

Landed in the emitter as `3c66a6a` on 2026-10-09. A dotted friendly name gives the C# its last segment, so the prefix
names only the OpenAPI schema. The rule differs from the plan: "always the declared name" would also have dropped
TCGC's generated names of template instances (`Page<User>` → `PageUser`), so a friendly name without a dot keeps
working as before. The fix runs in the emitter's `updateCodeModel` hook, ahead of the plugin's name comparisons
(`ValidationProblemDetails`, `ProblemDetails`). Tested by dotted friendly names on the `typed-ids` scalars and on
`feature-namespaces`' `Group`, whose snapshots stay unchanged.

Open: a test that a template instance with a friendly name without a dot keeps that name in C#.

## 2. `Http.` friendly names on the library's types (done)

In the same commit, with `uuid` added (`Http.uuid`), which the table missed. It is breaking for OpenAPI consumers, so
the commit is `feat!`. The rule: a schema of the service namespace stays bare, every other one carries the last segment
of its namespace (`UserManagement.GroupResponse`, `Primitives.UserId`, `Http.ProblemDetails`).

## 3. In this repository after the bump (done, staged 2026-10-09)

The typed ids carry `@friendlyName("Primitives.<Name>")`, and `openapi.yaml` changed in 61 lines (`Primitives.*`,
`Http.*`), with no generated C# file. The naming rule is in `docs/spec-first.md` (**Namespaces**) and
`.claude/rules/spec-first.md`.

Open: the optional guard in `scripts/gen.mjs` that compares the installed emitter with the integrity in
`package-lock.json` and stops on a mismatch, against a stale `node_modules` after a tgz change.

## 4. QUERY in the OpenAPI document (when upstream lands)

`users.tsp` declares the search `@httpQuery` and `@post`, since TypeSpec's `HttpVerb` has no QUERY (still in 1.17). The
emitter generates QUERY on both ends (`MapMethods(…, [HttpMethods.Query])`, `HttpMethod.Query`), the stock `openapi3`
writes `post:`. Checked 2026-10-08: `POST /users` answers 405, `QUERY /users` 401, and Scalar's "Try it" sends POST.

Upstream: [microsoft/typespec#11171](https://github.com/microsoft/typespec/issues/11171), the QUERY method (RFC 10008)
with OpenAPI 3.2 `query` operations, open, milestone October 2026. Once it ships, the spec uses the native verb,
`@httpQuery` leaves the library and the emitter, and `docs/spec-first.md` (**Spec style**) and its rule follow. No
interim patch: moving `post:` to `query:` in `scripts/gen.mjs` would work, but Scalar's support for `query:` is untested.

## 5. Server-sent events (done in the emitter working copy, 2026-10-09)

An operation answering `SSEStream<…>` over an `@events` union of one variant becomes an `IStreamQuery<Data>` (or
`IStreamCommand`), an endpoint over `TypedResults.ServerSentEvents` through the generated `ServerEventSupport`
(keepalive every 15 s, settable, the source started before the response opens), and a client method that returns the
generated `ServerEventReader<T>` once the stream is open: it skips unknown event types, ends after 30 s of silence and
owns the connection, and `ToOutcome()` covers it. The route is free, the template uses a `:watch` per feature as
`sse.md` planned, without the `/events` union of the old sketch. The event names, which the parent drops, travel as
the synthetic `@serverSentEvents` decorator. Other shapes stop the run with `server-sent-events-unsupported`: several
event types, an unnamed event, a terminal event, an envelope, data that is no JSON model, a parameter outside path and
query, a response beside the stream. TCGC's `union-enums-invalid-kind` warning on an `@events` union is dropped.
Tested by the house scenario `server-sent-events` and six round trips over TestHost.

Open: a modeled problem on a stream (a 404 before it opens), several event types per stream (a C# union), and a test
that the diagnostics fire, which needs a harness for the JS checks.

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

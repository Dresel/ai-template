---
paths:
  - "src/spec/**"
  - "src/*/spec/**"
  - "src/features/*/spec/**"
  - "src/features/*/*.{Api,Client,Shared}/**"
  - "**/tspconfig.yaml"
  - "**/generated/**"
  - "**/openapi.yaml"
  - "src/*.Primitives/**"
  - "src/*.Http/**"
  - "src/*/*.{Client,Shared}/**"
  - "src/*/*.Api/Features/**"
  - "src/*.Api.Foundation/Realtime/**"
  - "package.json"
  - "scripts/gen.mjs"
  - ".npm/**"
---
# Spec-first APIs

- Change an API in `src/<vertical>/spec/`, one kebab-case folder per slice. A slice with several interfaces declares a
  namespace below the service namespace. A feature's API is a service of its own in `src/features/<name>/spec/api.tsp`.
- A spec folder holds a `tspconfig.yaml` with `kind: project` and `entrypoint: api.tsp` (`http.tsp` in `src/spec/`),
  so the editor's language server compiles every file below it through its entrypoint.
- Every contracts, api and client `tspconfig.yaml` of the Admin vertical and the features sets
  `library-namespace: FocusTemplate.Http`. Code shared across APIs takes the library types from there.
- Run `npm run gen` at the repository root after every spec change and every emitter bump. `generated/` and
  `openapi.yaml` go into the same commit as the spec change.
- Never edit a file under `generated/`. `tsp-output/` is disposable.
- Typed ids are `@typedId` scalars in `src/spec/primitives.tsp`, a feature's in its `spec/primitives.tsp`, each with
  `@friendlyName("Primitives.<Name>")`. Permissions are `@permissions` enums in the slice's or the feature's
  `permissions.tsp`. The spine's `primitives.tsp` imports each slice's permissions and each feature's `primitives.tsp`.
- Identifiers use the `uuid` scalar. Error responses are `Problem<Status>`.
- A handler answers a modeled status with the case record of its result union and throws only for what is unhandled.
- Rules the spec cannot state go into the handler, or into a validator registered as `IValidator<T>` that includes the
  generated one. Keys come from the generated paths (`AdminPaths.…`).
- An operation behind a form declares the `ValidationProblem` alias.
- A stream of change signals is `watch(): SSEStream<…Events>` on a `:watch` route, over an `@events` union of one
  variant. Its handler derives from `EventStreamHandler<TQuery, TSignal>`. A filter overrides `MatchesAsync`, which
  runs once per stream and may query, and the lambda it returns runs per signal without querying.
- A handler that changes a user's permissions, or what a `MatchesAsync` reads, calls `SignalBackplane.CloseStreamsFor`
  with the affected users after `SaveChangesAsync`.
- A command handler publishes a signal through `SignalBackplane.Publish` after `SaveChangesAsync`, and a new signal
  type is registered with `AddSignal<T>()`.
- Routes are kebab-case plural nouns, and an action that is no CRUD is a custom method after a colon. A list that searches,
  sorts or pages is `@httpQuery` over a request spreading `PageQuery`, sorted by an enum.
- DTOs carry a `Request` or `Response` suffix. DELETE of a missing resource answers 204.
- An optional member's default is written in the spec, and handlers and clients read it as it arrives.
- `/** */` comments are written for API callers, `//` comments hold spec rationale.
- Consumers reference the vertical's `Client` project and call its generated clients.
- Entities stay on the server: the handler maps entity to DTO. Computed members of a DTO go into a hand-written
  partial next to the project file. The two `Shared` projects never reference each other.
- Bump the emitter in the order the doc gives.

Background: [docs/spec-first.md](../../docs/spec-first.md).
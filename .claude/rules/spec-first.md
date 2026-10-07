---
paths:
  - "src/spec/**"
  - "src/*/spec/**"
  - "**/tspconfig.yaml"
  - "**/generated/**"
  - "**/openapi.yaml"
  - "src/*.Primitives/**"
  - "src/*/*.{Client,Shared}/**"
  - "src/*/*.Api/Features/**"
  - "package.json"
  - "scripts/gen.mjs"
  - ".npm/**"
---
# Spec-first APIs

- Change an API in `src/<vertical>/spec/`, one kebab-case folder per slice. A slice with several interfaces declares a
  namespace below the service namespace.
- Run `npm run gen` at the repository root after every spec change and every emitter bump. `generated/` and
  `openapi.yaml` go into the same commit as the spec change.
- Never edit a file under `generated/`. `tsp-output/` is disposable.
- Typed ids are `@typedId` scalars in `src/spec/primitives.tsp`, and permissions are `@permissions` enums in
  `src/spec/permissions/`.
- Identifiers use the `uuid` scalar. Error responses are `Problem<Status>`.
- A handler answers a modeled status with the case record of its result union and throws only for what is unhandled.
- Rules the spec cannot state go into the handler, or into a validator registered as `IValidator<T>` that includes the
  generated one. Keys come from the generated paths (`AdminPaths.…`).
- An operation behind a form declares the `ValidationProblem` alias.
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
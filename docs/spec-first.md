# Spec-first APIs

What to follow when changing these files: [the rule](../.claude/rules/spec-first.md).

Both APIs are generated from TypeSpec by `@spatialfocus/typespec-http-csharp-slim`, consumed as the packed tgz under
`.npm/`. The emitter lives in its own repository `typespec-http-csharp-slim`, where its output is documented and
tested. This repository only checks that regeneration is clean. It also provides the TypeSpec library every contract
imports (`namespace SpatialFocus.Http`).

## Toolchain

- The root `package.json` pins `@typespec/compiler`, `@typespec/http`, `@typespec/openapi`, `@typespec/openapi3`
  (OpenAPI 3.2) and the emitter as a `file:` dependency on `.npm/<name>-<version>.tgz`. A plain `npm install` keeps the
  old contents while the version stays the same, hence the bump order below.
- `npm run gen` (`scripts/gen.mjs`) runs one `tsp compile` per C# project's `tspconfig.yaml`, and the parent
  emitter's scaffolding lands in the disposable `tsp-output/`.
- Each spec folder (`src/spec/`, `src/<vertical>/spec/`, `src/admin/user-management/spec/`, `src/features/<name>/spec/`)
  holds the editor's
  `tspconfig.yaml`: `kind: project` with the `entrypoint` the TypeSpec language server compiles for every file below it
  (`api.tsp`, and `http.tsp` in `src/spec/`). Without it the server compiles an open file on its own, and a slice's file
  reports every type its `api.tsp` imports as unknown. A file the entrypoint does not import (`paging.tsp`) the server
  compiles on its own. `npm run gen` leaves these configs out, and `tsp compile <spec folder>` checks a spec without
  emitting.
- Each spec folder is also a project that builds nothing (`SpatialFocus.Spec`, `FocusTemplate.<Vertical>.Spec`,
  `SpatialFocus.Admin.UserManagement.Spec`, `SpatialFocus.<Feature>.Spec`) on the `Microsoft.Build.NoTargets` SDK,
  pinned in `global.json`. Visual Studio shows the folder as it is on disk, next to the vertical's projects, and a file
  created outside it appears without an edit to a project or the solution. It restores without a lock file, since its only package is the global analyzer.
- `SpatialFocus.Http` holds the types every Admin project generates alike, written once from `src/spec/http.tsp`
  with `output-type: library`: `ProblemDetails`, the validation problem with `Violation` and `ViolationSeverity`,
  `PermissionRequirement` with `RequiresPermissionAttribute`, and the client's `ApiFailure`, `ApiOutcome`,
  `IProblemOf`, `Success` and `ServerEventReader`. The Admin vertical's `tspconfig.yaml` files set
  `library-namespace: SpatialFocus.Http`, so their output references these and writes none, and code shared across
  the projects of several APIs (the Web foundation, the permission handlers) accepts one type for each. The Public
  vertical keeps its own copies, so its mobile app carries no ASP.NET authorization package.
- The emitter wraps `@typespec/http-client-csharp` (alpha, daily builds) and ships its .NET plugin inside the package,
  so nothing in this solution compiles against those assemblies.
- `Mediator.Abstractions` with `Mediator.SourceGenerator` (martinothamar, MIT): the generated endpoints dispatch
  `IQuery<T>` and `ICommand<T>` records to hand-written handlers.
- The OpenAPI document is emitted from the spec into each Api project, which ships it as `openapi/<name>.yaml`
  (`admin`, `public`, `user-management`, a feature's `weather`). An API serves every document in its output, so the
  Admin API answers `/openapi/admin.yaml`, `/openapi/user-management.yaml` and `/openapi/weather.yaml`, the Public API
  `/openapi/public.yaml`. Any other name is a 404.
- **API reference**: `MapApiReference("admin")` in the service defaults adds `Scalar.AspNetCore` (MIT, trimmable and
  AOT-compatible, its assets served from the assembly) at `/scalar`, with one entry per document and the API's own
  first. The Public API serves its document alone through `MapOpenApiDocuments()`. `ApiReference:Enabled` switches
  the page and the documents together, and unset the environment decides: on in Development, off elsewhere. A deployed
  stage for external developers sets `ApiReference__Enabled=true` in its configuration (the stage's ConfigMap in
  `SpatialFocus/Infrastructure`), production leaves it unset. Access control on such a stage belongs to its ingress
  (basic auth or an IP allowlist, as Traefik middleware). "Try it" sends the bearer token the user pastes, the
  documents declaring no other scheme yet, and a search shows as `POST` until OpenAPI can write QUERY. The documents
  go out with `Cache-Control: no-cache`, so a reload after a spec change shows the new one.
- **Examples**: `@example` on a model and `@opExample` on an operation put real values into the document, from one
  `const` both share (the weather forecasts so far). The parameters of an `@opExample` reach the document once the Api
  project's `@typespec/openapi3` options set `experimental-parameter-examples: data`, an option the emitter still calls
  experimental, so far in `Weather.Api` and `Admin.UserManagement.Api`. A typed id takes its example there like any
  other value.
- **Server-sent events** come from the spec: an operation answering `SSEStream<…>` over an `@events` union of one
  variant (`watch(): SSEStream<GroupEvents>`, `GET /groups:watch`). The events are signals of what changed, and a page
  reads the item again. Generated: the stream query (`IStreamQuery<GroupChanged>`), the endpoint over
  `TypedResults.ServerSentEvents` with a keepalive event every 15 s (`ServerEventSupport`), and a client method that
  returns a `ServerEventReader<T>` once the stream is open, which ends after 30 s of silence and owns the connection.
  By hand, in `Realtime/` of `SpatialFocus.Api.Foundation`, so a feature's API uses it too. Its root is the local SSE
  side: the handler on `EventStreamHandler<TQuery, TSignal>`,
  authorized when the stream opens, subscribes to the process's `EventStreams<T>` of its signal type, then loads its
  filter once through `MatchesAsync`, which may query, and applies it to each signal in the stream's own request.
  Subscribing comes first, so nothing published while the filter loads is lost. `Realtime/Backplane/` is Redis Pub/Sub
  between the API processes (the AppHost's `redis`): command handlers publish through `SignalBackplane` to every
  process, its own included, where a `BackplaneSubscription<T>` hands each signal to `EventStreams<T>`. It publishes
  fire and forget, so a command neither waits for Redis nor fails with it. `AddSignal<T>()` registers both parts of a
  signal type. The stream ends with the access token, after 15 minutes at the latest, when a handler changes the
  subscriber's permissions or what `MatchesAsync` read (`SignalBackplane.CloseStreamsFor`), and when the Redis
  subscription reconnects, so the reconnect is authorized again and the page reloads what it missed. On the page side
  `LiveUpdates<T>`, scoped, gives each page's view model a stream of its own, which closes with the page and reconnects
  after any end. It raises `Connected` after every connect, the first included, and the groups list reads everything
  again then: the server subscribed before it answered, so a change misses neither that read nor the stream.
  `UseUnbufferedEventStreams` sends `X-Accel-Buffering: no`, so nginx passes each event on. Over plain HTTP/1.1
  (`http://localhost:5770`) every open stream holds one of the six connections a browser allows a host, across its
  tabs. The ingress (`https://localhost:7770`) multiplexes them over HTTP/2.

## Layout

- One contract per vertical in `src/<vertical>/spec/`. `api.tsp` carries the service metadata and imports one folder
  per feature slice (`weather-forecasts/weather-forecasts.tsp`, `demo-profiles/demo-profiles.tsp`). Files and
  folders are kebab-case, like the routes. The Admin `api.tsp` also declares `@useAuth`, while the Public API stays anonymous
  for now.
- A slice with one interface stays in the service namespace (`FocusTemplate.Admin`), a slice with several declares a
  namespace below it (`FocusTemplate.Admin.<Feature>`), its feature.
- `src/spec/` holds what verticals can share: `primitives.tsp` (the typed ids, namespace `SpatialFocus.Primitives`,
  no service) and `paging.tsp` (`PageQuery`, spread into a search request). `primitives.tsp` also imports user
  management's `permissions.tsp`. Today the Admin contract, user management and the weather feature import it.
- `src/admin/spec/primitives.tsp` is the Admin product's composition of permissions: it imports the spine's
  `primitives.tsp` and each included feature's, so adding or removing a feature there is one import line.
- Three projects per vertical generate, each with its own `tspconfig.yaml` and `generated/` folder: the Api project
  (`output-type: api`), the Shared project (`contracts`) and the Client project (`client`). Everything else consumes
  them by project reference, and generated files are never linked across projects.
- A feature has a contract of its own in `src/features/<name>/spec/`: `api.tsp` is a `@service` with `@useAuth`
  (`SpatialFocus.Weather`), imports its own `primitives.tsp` and one file per interface, and its three projects
  generate as a vertical's do (`SpatialFocus.Weather.Api.{interface}`, `SpatialFocus.Weather.Shared`,
  `SpatialFocus.Weather.Client.{interface}`). Its own JSON context, validators and paths carry the service's name
  (`WeatherJsonContext`, `AddWeatherValidators()`), and its `openapi.yaml` sits in its Api project. Its typed ids sit
  beside it in a `primitives.tsp` of its own, in namespace `SpatialFocus.Weather.Primitives` below the service, which
  imports the spine's `primitives.tsp` and its `permissions.tsp` (`SpatialFocus.Weather.Primitives.Permissions`). Its
  Api project sets `omit-unreachable-types: true` for `@typespec/openapi3`, so the ids and the permissions enum below
  the service appear in the document only where a model uses them.
- User management, which every Admin host includes, has the same layout in `src/admin/user-management/`: the service
  `SpatialFocus.Admin.UserManagement` with the interfaces `Users` and `Groups`
  (`SpatialFocus.Admin.UserManagement.Api.{interface}`, `.Shared`, `.Client.{interface}`), `UserManagementJsonContext`,
  and its document shipped as `openapi/user-management.yaml`. Its ids are the spine's, so only its `permissions.tsp` is
  imported by `src/spec/primitives.tsp`. `npm run gen` compiles a folder with a `spec/api.tsp` of its own inside a
  vertical as it does a feature.
- **Registering and mapping** an API is generated per service: `{Service}Endpoints` in the Api namespace without the
  placeholders, with `Add{Service}Endpoints()` (the JSON context first in the minimal API's resolver chain, and the
  validators) and `Map{Service}Endpoints()` (every interface). A feature namespace with `{feature}` in the pattern gets
  `{Feature}Endpoints` with `Map{Feature}Endpoints()`. The Admin API calls `AddAdminEndpoints()` and
  `MapDemoProfilesEndpoints()`, user management `AddUserManagementEndpoints()` in `AddUserManagement()` and
  `MapUserManagementEndpoints()` in the host, and the weather feature `AddWeatherEndpoints()` in `AddWeather()` and
  `MapWeatherEndpoints()` in the host.

## Typed ids

A primitives project compiles a `primitives.tsp` (`output-type: primitives`) into Vogen value objects:
`[ValueObject<T>] [Instance("Unspecified", …)] public readonly partial struct` per `@typedId` scalar, plus one
`{Enum}Permissions` class per permissions enum (see [authorization](authorization.md)). Its `primitives-namespace` names
the one namespace it writes, and what its spec imports from elsewhere it references:

- `SpatialFocus.Primitives` writes the spine's ids, `Permission` (a string id) and `UserManagementPermissions` from
  `src/spec/primitives.tsp`.
- A feature's `*.Primitives` project (`SpatialFocus.Weather.Primitives`) writes the feature's ids and its permission
  class from the feature's `spec/primitives.tsp`, into an assembly of the feature's own. A permission class goes into
  the enum's namespace without its last segment `Permissions`, so a feature's sits beside its ids.
- `FocusTemplate.Admin.Primitives` writes no id. Its `permission-list: AdminPermissions` lists every permission its spec
  `src/admin/spec/primitives.tsp` imports, which is what the Admin product's groups can be granted.

`scripts/gen.mjs` compiles a feature's or a vertical's `*.Primitives` project from the `spec/primitives.tsp` beside it.
`Data` and every contract project (`Admin.Shared`, `Admin.UserManagement.Shared`, a feature's `Shared`) reference the
primitives they use, so one `GroupId` serves the domain and every contract. `Data` also runs Vogen itself,
for the EF Core converters it marks with `[EfCoreConverter<T>]`. A `@typedId` declared inside a service namespace is an
emitter error.

## Namespaces

- `api-namespace: FocusTemplate.<V>.Api.Features.{feature}.{interface}` puts a slice's generated code in the namespace
  of its hand-written handlers, and `client-namespace: FocusTemplate.<V>.Client.{feature}.{interface}` does the same on
  the consumer side. `{feature}` is empty for an interface in the service namespace: `Features.DemoProfiles`, the
  Public API's `Features.WeatherForecasts`.
- `contracts-namespace: FocusTemplate.<V>.Shared.{feature}`, in all three of the vertical's `tspconfig.yaml`, gives a
  feature's models a namespace of their own (`FocusTemplate.Admin.Shared.<Feature>`).
- What every slice shares (`NotFound`, `ApiClientSupport`, the `{Status}Problem` cases, the outcomes, the JSON context,
  the paths) lands in the namespace without the placeholders.
- OpenAPI names a schema of the service namespace bare and every other one after the last segment of its namespace: a
  feature's by itself (`<Feature>.<Name>`), the typed ids through `@friendlyName("Primitives.<Name>")`
  (`Primitives.GroupId`), the emitter's library types through its own friendly names (`Http.ValidationProblemDetails`,
  `Http.uuid`). The emitter keeps a friendly name's prefix out of the C# names.

## Server

- Hand-written code per slice lives in the Api project's `Features/<Slice>/` (`Features/<Feature>/<Interface>/` for an
  interface below a slice's own namespace, user management's in `Admin.UserManagement.Api/<Interface>/`, a feature
  package's in `<Feature>.Api/<Interface>/`): the Mediator handlers, and where
  needed a partial class implementing the generated `ConfigureGroup` / `Configure{Op}` hooks (rate limiting, caching).
  Authorization comes from the spec, never from a hook.
- **Errors are values**: a handler returns the generated result union and answers a modeled status with its case
  record (`new NotFound("…")`). Anything unhandled becomes a 500 problem response through `AddProblemDetails()`.
- **Validation**: the Api and Shared projects of the Admin vertical, user management and the features generate with
  `validation: fluentvalidation`, so constraint
  decorators (`@maxLength`, `@minValue`, …) become FluentValidation validators, and a filter on each validated endpoint
  answers a violation with a 400 before the handler runs. `AddAdminEndpoints()` in `Program.cs` registers them through
  `AddAdminValidators()`, and mapping the endpoints without them fails at startup. Rules the spec cannot state stay in
  the handler, or go into a validator registered as `IValidator<T>` that includes the generated one. Those that need no
  server data can live in `Admin.Shared`, so the forms run them too (see [web forms](web-forms.md)). The generated rules
  ask `ContractTexts` in `SpatialFocus.Http`, one hook for every service that shares the library, for the names of
  their members and for the messages of their patterns, see [localization](localization.md).
- The 400 carries `errors` by wire path and `violations` (key, code, severity, message, args), a typed
  `ValidationProblem` case on the client. An operation behind a form declares the `ValidationProblem` alias, since under
  `Problem<400>` the same members would arrive as untyped extensions. With `RespectRequiredConstructorParameters` on, a
  missing required member is a model-binding 400 without keys.

## Clients

- A consumer references `FocusTemplate.<V>.Client` and registers the generated client with its typed-`HttpClient`
  helper (`AddProxiedHttpClient` in the Web app, whose `BaseAddress` carries the BFF prefix, and service discovery in
  Mobile). Extra members go into a hand-written partial next to the consumer's project file.
- A view model turns a call's result union into an outcome with the generated `ToOutcome()` (see **Answers** in
  [web pages](web-pages.md)). A status added to the spec therefore arrives as an `ApiFailure` until a caller handles it
  itself.

## Spec style

- Routes are kebab-case plural nouns (`/weather-forecasts`). An action that is no CRUD is a custom method after a colon
  (`POST /users/{id}:deactivate`, AIP-136), and `/users/me` is the alias of the signed-in user.
- A list that searches, sorts or pages is `@httpQuery` (HTTP QUERY with a body) over a request spreading `PageQuery`,
  sorted by an enum the handler switches over.
- DTOs carry a `Request` or `Response` suffix, which keeps them apart from the like-named entities in
  `SpatialFocus.Data`. They live in the vertical's `Shared` project, and the two `Shared` projects never reference each
  other. Entities stay on the server: the handler maps entity to DTO. Computed members go into hand-written partials
  next to the project file.
- An optional member's default (`top?: int32 = 10`) reaches both ends as the C# default (`int Top = 10`). An optional
  member without one stays off the wire while null, and an explicit `null` is a 400.
- DELETE of a missing resource answers 204, so a retrying client stays idempotent. JSON is camelCase.
- `/** */` comments are published to OpenAPI and the generated XML summaries, so they are written for API callers.
  `//` comments hold spec rationale, emitter workarounds included.
- Emitter traps that fail silently: identifiers use the shared `uuid` scalar (`@format("uuid")` on a string does not
  make a `Guid`), and error responses are `Problem<Status>` (an `@error` model is not picked up).

## Bumping the emitter

In the emitter repository, `npm run gen` and `npm test` first, which must leave no tracked changes, then
`npm pack --pack-destination <this repository>/.npm`. Here, `npm install ./.npm/<name>-<version>.tgz` and `npm run gen`,
and the regenerated `generated/` folders and `openapi.yaml` go into the same commit.
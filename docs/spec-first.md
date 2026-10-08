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
- `npm run gen` (`scripts/gen.mjs`) runs one `tsp compile` per project `tspconfig.yaml`, and the parent emitter's
  scaffolding lands in the disposable `tsp-output/`.
- The emitter wraps `@typespec/http-client-csharp` (alpha, daily builds) and ships its .NET plugin inside the package,
  so nothing in this solution compiles against those assemblies.
- `Mediator.Abstractions` with `Mediator.SourceGenerator` (martinothamar, MIT): the generated endpoints dispatch
  `IQuery<T>` and `ICommand<T>` records to hand-written handlers.
- The OpenAPI document is emitted from the spec, and each API serves the file in Development (`/openapi/v1.yaml`).
- **Server-sent events** the emitter does not generate yet. `GET /groups:watch` is a draft by hand, missing from
  `openapi.yaml`: the endpoint (`GroupsWatchEndpoint`), a Mediator stream query and its handler, an in-memory
  `SignalHub<T>` (one API process) the command handlers publish to, the event record `GroupChanged` with a partial of
  `AdminJsonContext`, `GroupsClient.WatchAsync` as a partial of the generated client, and on the page side a
  `ChangeFeed<T>`, one stream per tab, open only while a page listens, that reconnects and makes the groups list read
  everything again. Over plain HTTP/1.1 (`http://localhost:5770`) every open stream holds one of the six connections a
  browser allows a host, across its tabs. The ingress (`https://localhost:7770`) multiplexes them over HTTP/2.

## Layout

- One contract per vertical in `src/<vertical>/spec/`. `api.tsp` carries the service metadata and imports one folder
  per feature slice (`weather-forecasts/weather-forecasts.tsp`, `user-management/users.tsp` + `groups.tsp`). Files and
  folders are kebab-case, like the routes. The Admin `api.tsp` also declares `@useAuth`, while the Public API stays anonymous
  for now.
- A slice with one interface stays in the service namespace (`FocusTemplate.Admin`), a slice with several declares a
  namespace below it (`FocusTemplate.Admin.UserManagement`), its feature.
- `src/spec/` holds what verticals can share: `primitives.tsp` (the typed ids, namespace `FocusTemplate.Primitives`,
  no service), `paging.tsp` (`PageQuery`, spread into a search request) and `permissions/`. Today only the Admin
  contract imports them.
- Three projects per vertical generate, each with its own `tspconfig.yaml` and `generated/` folder: the Api project
  (`output-type: api`), the Shared project (`contracts`) and the Client project (`client`). Everything else consumes
  them by project reference, and generated files are never linked across projects.

## Typed ids

`FocusTemplate.Primitives` compiles `src/spec/primitives.tsp` (`output-type: primitives`) into Vogen value objects:
`[ValueObject<T>] [Instance("Unspecified", …)] public readonly partial struct` per `@typedId` scalar, plus `Permission`
(a string id) with one `{Enum}Permissions` class per permissions enum (see [authorization](authorization.md)). `Data`
and `Admin.Shared` reference it, so one `GroupId` serves the domain and every contract. `Data` also runs Vogen itself,
for the EF Core converters it marks with `[EfCoreConverter<T>]`. A `@typedId` declared inside a service namespace is an
emitter error.

## Namespaces

- `api-namespace: FocusTemplate.<V>.Api.Features.{feature}.{interface}` puts a slice's generated code in the namespace
  of its hand-written handlers, and `client-namespace: FocusTemplate.<V>.Client.{feature}.{interface}` does the same on
  the consumer side. `{feature}` is empty for an interface in the service namespace: `Features.WeatherForecasts`,
  `Features.UserManagement.Users`.
- `contracts-namespace: FocusTemplate.<V>.Shared.{feature}`, in all three of the vertical's `tspconfig.yaml`, gives a
  feature's models a namespace of their own (`FocusTemplate.Admin.Shared.UserManagement`).
- What every slice shares (`NotFound`, `ApiClientSupport`, the `{Status}Problem` cases, the outcomes, the JSON context,
  the paths) lands in the namespace without the placeholders. OpenAPI names a feature's schemas after it
  (`UserManagement.GroupResponse`).

## Server

- Hand-written code per slice lives in the Api project's `Features/<Slice>/` (`Features/UserManagement/Users/` for a
  feature's interface): the Mediator handlers, and where needed a partial class implementing the generated
  `ConfigureGroup` / `Configure{Op}` hooks (rate limiting, caching). Authorization comes from the spec, never from a
  hook.
- **Errors are values**: a handler returns the generated result union and answers a modeled status with its case
  record (`new NotFound("…")`). Anything unhandled becomes a 500 problem response through `AddProblemDetails()`.
- **Validation**: the Admin Api and Shared projects generate with `validation: fluentvalidation`, so constraint
  decorators (`@maxLength`, `@minValue`, …) become FluentValidation validators, and a filter on each validated endpoint
  answers a violation with a 400 before the handler runs. `AddAdminValidators()` in `Program.cs` registers them, and
  mapping the endpoints without it fails at startup. Rules the spec cannot state stay in the handler, or go into a
  validator registered as `IValidator<T>` that includes the generated one. Those that need no server data can live in
  `Admin.Shared`, so the forms run them too (see [web forms](web-forms.md)). The generated rules ask the generated
  `AdminTexts` hook for the names of their members and for the messages of their patterns, see
  [localization](localization.md).
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
  `FocusTemplate.Data`. They live in the vertical's `Shared` project, and the two `Shared` projects never reference each
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
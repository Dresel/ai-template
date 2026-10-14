---
name: feature-slice
description: "Add a feature slice to the Admin vertical end to end, or a new operation to an existing one: spec, permissions, typed ids, entity and migration, Mediator handlers, generated client, pages and forms, tests. Use when the task is a new API resource, a new page backed by the API, a CRUD area, or a new operation on an existing slice."
---

# Feature slice

The order of the work and every place it touches. The reasoning lives in the docs each step links, and the rules
that load with the files apply as usual.

A slice lives inside the Admin vertical's projects. Something meant to move between hosts is a feature package of its
own under `src/features/<name>/` instead, built like `src/features/weather/`, see
[architecture](../../../docs/architecture.md).

## Copy from

| What the slice needs | Reference |
|---|---|
| A read-only list | `WeatherForecasts` in the weather feature (`spec/weather-forecasts.tsp`, `WeatherForecasts/` in its Api and Web) |
| An action only the owner, or a permission, may take | `Stations` in the weather feature: `StationsRenameHandler`, and `MayRename` in `Stations/ListPage.razor` |
| A list that searches, sorts and pages | `Users` in user management (`src/admin/user-management/`): `UsersSearchHandler`, `Users/ListPage.razor`, `Users/ListPageViewModel` |
| Create and update through a form in a dialog | `Groups` in user management: `GroupsCreateHandler`, `GroupsUpdateHandler`, `Groups/FormDialog.razor`, `Form`, `FormValidator`, `Mapper` |
| A detail page with actions | `Groups/DetailPage.razor`, `Groups/DetailPageViewModel` |
| A list that stays current when others change it | `Groups`: `watch()` in `groups.tsp`, `GroupsWatchHandler`, the `Publish` calls in the command handlers, `Groups/ListPageViewModel` |
| Rules the spec cannot state, async server checks | `DemoProfiles` (`DemoProfileCustomRules`, `DemoProfileRequestValidator`) |
| Several interfaces behind one service | user management, a service of its own: `spec/api.tsp` importing `users.tsp` and `groups.tsp`, one folder per interface in its Api and Web projects. Inside the Admin vertical the same layout goes below `Features/<Feature>/` (step 1) |

## 1. Decide

- Names: a kebab-case plural route and folder (`src/admin/spec/<slice>/`), an interface in PascalCase, DTOs with a
  `Request` or `Response` suffix.
- One interface stays in the service namespace. Several get a feature namespace (`namespace FocusTemplate.Admin.<Feature>;`),
  which puts the code into `Features/<Feature>/<Interface>/`.
- Which permission each operation requires (`View…`, `Manage…`), and which test level specifies the behaviour first
  (AGENTS.md, **Development loop**).

## 2. Contract ([spec-first](../../../docs/spec-first.md))

1. A new id: `@typedId scalar <X>Id extends uuid;` with a doc comment in `src/admin/spec/primitives.tsp`, namespace
   `FocusTemplate.Admin.Primitives`. The first id also brings the `Vogen` package into `FocusTemplate.Admin.Primitives`.
2. New permissions: an enum in `src/admin/spec/<slice>/permissions.tsp`, namespace
   `FocusTemplate.Admin.Primitives.Permissions` (copy user management's in `src/admin/user-management/spec/`), imported
   in `src/admin/spec/primitives.tsp`, which puts them into `AdminPermissions`.
3. The slice's file in `src/admin/spec/<slice>/`, imported in `src/admin/spec/api.tsp`: `@requiresPermission` per
   operation or interface, `Problem<404 | 409>` for what the handler refuses, the `ValidationProblem` alias behind a
   form, `@httpQuery` over a request spreading `PageQuery` for a searchable list, defaults in the spec. A list that
   stays current gets `watch(): SSEStream<…Events>` on a `:watch` route, over an `@events` union of one variant whose
   model names what changed (`GroupChanged`: the id and `created`, `updated` or `deleted`).
4. `npm run gen` at the repository root.

## 3. Data ([database](../../../docs/database.md))

A slice of the Admin product keeps its tables in the product's own context in `FocusTemplate.Data`, schema `admin`. A
self-contained capability that hosts drop in is a feature package instead, laid out like weather (see
[architecture](../../../docs/architecture.md)).

1. The first slice with entities creates the product context once, each piece as weather's has it: `AdminDbContextBase`
   deriving from `FoundationDbContext` in `src/FocusTemplate.Data/Admin/`, `AdminDbContext` and
   `ReadOnlyAdminDbContext`, their design-time factory beside them, the registration in `Admin.Api`, and an
   `admin-migrations` resource in the AppHost that waits for the spine's `migrations`.
2. The entity and its `IEntityTypeConfiguration` in `src/FocusTemplate.Data/Admin/Entities/`, the configuration in the
   context's `ConfigureModel`, a `DbSet`, `IAuditable` when its changes are audited, and `UserReference` for a foreign
   key to the users.
3. A new id: its `[EfCoreConverter<X>]` and `HaveSentinel(X.Unspecified)` in the context's `ConfigureIds`.
4. The migration, with the AppHost stopped:
   `dotnet dotnet-ef migrations add <Name> --project src/FocusTemplate.Data --startup-project src/admin/FocusTemplate.Admin.Api --context AdminDbContext`.
   New permissions are a migration of the spine's context as well, the same command with `--context AppDbContext`:
   check that it inserts their rows and grants them to `Administrators`.
5. Dev data, if the slice needs it: a seed part that checks for its own rows.

## 4. Handlers

1. One handler per operation in the Api project's `Features/<Slice>/`, in the generated namespace: a query handler takes
   `ReadOnlyAppDbContext`, a command handler `AppDbContext`. It returns the union's cases: 404 and 409 as records, a
   field's rule as a validation problem keyed with the generated paths.
2. `endpoints.Map<Interface>Endpoints();` in `src/admin/FocusTemplate.Admin.Api/Program.cs`. Mediator finds the
   handlers itself. Services or a validator of the slice's own go into an `Add<Slice>()` called there.
3. Messages a user reads (`NotFound`, `Conflict`, a handler's validation failure) come from the interface's resx pair,
   `Features/<Slice>/<Interface>.resx` and `.de.resx`, through `I<Interface>Localizations localizations`
   ([localization](../../../docs/localization.md)).
4. A watch: its handler derives from `EventStreamHandler<TQuery, TSignal>`, `builder.Services.AddSignal<T>()` in
   `Program.cs` registers the signal type, and every command handler that changes what the list shows calls
   `backplane.Publish(…)` after `SaveChangesAsync` (`SignalBackplane`). A handler that changes a user's permissions
   calls `backplane.CloseStreamsFor(…)` with the users it affects.

## 5. Web ([web pages](../../../docs/web-pages.md), [web forms](../../../docs/web-forms.md))

1. `Features/<Slice>/ServiceCollectionExtensions.cs` with `Add<Slice>()`: `AddProxiedHttpClient<<Interface>Client>(environment, ResourceNames.AdminApi)`,
   the view models `AddScoped`, form validators as `IValidator<Form>`. `Program.cs` of the Web project calls it.
2. Pages with `public const string Path` / `PathOf(id)`, `@attribute [RequiresPermission(…Permissions.Names.…)]`,
   view models on `ViewModelPage<T>` with `AsyncCommand` loads and `ToOutcome()`, a `data-testid` on everything a test
   touches.
3. A form: `Form`, `FormValidator` applying the generated `{Model}Rules`, `Mapper`, `<AppForm>`.
4. The menu entry in `Shell/MainLayout.razor`, shown with `context.User.Has(<Permission>)`, its text in
   `MainLayout.resx`.
5. Every text in the component's resx pair (`@inject I<Component>Localizations L`). A new contract's field names and
   pattern messages in German go into `Admin.Shared/<Model>.de.resx` (keys `<Member>`, `<Member>.pattern`).
6. A list that stays current: `services.AddLiveUpdates(…)` in `Add<Slice>()` with the client's `WatchAsync`, and the
   view model takes the `LiveUpdates<T>` and reloads on `Received` and on `Connected`.

New permissions appear in the group editor by themselves, labelled from their names. Their German labels go into
`PermissionLabels.de.resx` of `Admin.UserManagement.Web`, keyed by slice and by permission.

## 6. Tests

- **Integration** (`tests/FocusTemplate.Admin.Api.IntegrationTests`, through the generated client): per operation the
  answer, the 404, each 409, the 400, and the 403 without the permission.
- **Unit** (`tests/FocusTemplate.Admin.Web.UnitTests` for a slice of the host, `tests/SpatialFocus.Web.UnitTests` for
  one of user management or a feature): a view model against a fake HTTP handler (`ListPageViewModelTests`), the
  form's validator.
- **E2E**: one flow through the BFF on `BffPageTest`, selected by `data-testid`, including the menu entry.
- `AuthenticationTests.EveryApiEndpointRequiresAuthorization` covers the new endpoints by itself.
- **A watch** (`GroupsWatchTests`): a change reaches an open stream, the 403 without the permission, and a stream that
  closes when its subscriber loses the permission.

## 7. Finish

1. `dotnet format` on the touched projects, `dotnet build FocusTemplate.slnx`, the tests (AGENTS.md, **Build, test,
   format**).
2. A statement in AGENTS.md, `docs/` or `.claude/rules/` the slice made wrong is corrected, and a new pattern goes into
   its doc.
3. One commit holds the spec, the generated code, `openapi.yaml` and the migration.
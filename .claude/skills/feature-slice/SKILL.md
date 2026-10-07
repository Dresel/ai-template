---
name: feature-slice
description: "Add a feature slice to the Admin vertical end to end, or a new operation to an existing one: spec, permissions, typed ids, entity and migration, Mediator handlers, generated client, pages and forms, tests. Use when the task is a new API resource, a new page backed by the API, a CRUD area, or a new operation on an existing slice."
---

# Feature slice

The order of the work and every place it touches. The reasoning lives in the docs each step links, and the rules
that load with the files apply as usual.

## Copy from

| What the slice needs | Reference |
|---|---|
| A read-only list | `WeatherForecasts` (spec, `Features/WeatherForecasts/` in Api and Web) |
| A list that searches, sorts and pages | `Users`: `UsersSearchHandler`, `Users/ListPage.razor`, `Users/ListPageViewModel` |
| Create and update through a form in a dialog | `Groups`: `GroupsCreateHandler`, `GroupsUpdateHandler`, `Groups/FormDialog.razor`, `Form`, `FormValidator`, `Mapper` |
| A detail page with actions | `Groups/DetailPage.razor`, `Groups/DetailPageViewModel` |
| Rules the spec cannot state, async server checks | `DemoProfiles` (`DemoProfileCustomRules`, `DemoProfileRequestValidator`) |
| Several interfaces in one feature | `UserManagement` (spec namespace, `Features/UserManagement/_Imports.razor`) |

## 1. Decide

- Names: a kebab-case plural route and folder (`src/admin/spec/<slice>/`), an interface in PascalCase, DTOs with a
  `Request` or `Response` suffix.
- One interface stays in the service namespace. Several get a feature namespace (`namespace FocusTemplate.Admin.<Feature>;`),
  which puts the code into `Features/<Feature>/<Interface>/`.
- Which permission each operation requires (`View…`, `Manage…`), and which test level specifies the behaviour first
  (AGENTS.md, **Development loop**).

## 2. Contract ([spec-first](../../../docs/spec-first.md))

1. A new id: `@typedId scalar <X>Id extends uuid;` with a doc comment in `src/spec/primitives.tsp`.
2. New permissions: an enum in `src/spec/permissions/<slice>.tsp` (copy `user-management.tsp`), imported in
   `src/spec/primitives.tsp`.
3. The slice's file in `src/admin/spec/<slice>/`, imported in `src/admin/spec/api.tsp`: `@requiresPermission` per
   operation or interface, `Problem<404 | 409>` for what the handler refuses, the `ValidationProblem` alias behind a
   form, `@httpQuery` over a request spreading `PageQuery` for a searchable list, defaults in the spec.
4. `npm run gen` at the repository root.

## 3. Data ([database](../../../docs/database.md))

1. The entity and its `IEntityTypeConfiguration` in `src/FocusTemplate.Data/Entities/`, the configuration in
   `ApplyEntityConfigurations` (`ModelBuilderExtensions`), a `DbSet` in `AppDbContextBase`, `IAuditable` when its
   changes are audited.
2. A new id: `[EfCoreConverter<X>]` in `VogenEfCoreConverters` and `HaveSentinel(X.Unspecified)` in
   `AppDbContextBase.ConfigureConventions`.
3. The migration, with the AppHost stopped:
   `dotnet dotnet-ef migrations add <Name> --project src/FocusTemplate.Data --startup-project src/FocusTemplate.Data --context AppDbContext`.
   With new permissions, check that it inserts their rows and grants them to `Administrators`.
4. Dev data, if the slice needs it: a part in `DevelopmentSeed` that checks for its own rows.

## 4. Handlers

1. One handler per operation in the Api project's `Features/<Slice>/`, in the generated namespace: a query handler takes
   `ReadOnlyAppDbContext`, a command handler `AppDbContext`. It returns the union's cases: 404 and 409 as records, a
   field's rule as a validation problem keyed with the generated paths.
2. `endpoints.Map<Interface>Endpoints();` in `src/admin/FocusTemplate.Admin.Api/Program.cs`. Mediator finds the
   handlers itself. Services or a validator of the slice's own go into an `Add<Slice>()` called there.
3. Messages a user reads (`NotFound`, `Conflict`, a handler's validation failure) come from the interface's resx pair,
   `Features/<Slice>/<Interface>.resx` and `.de.resx`, through `I<Interface>Localizations localizations`
   ([localization](../../../docs/localization.md)).

## 5. Web ([web pages](../../../docs/web-pages.md), [web forms](../../../docs/web-forms.md))

1. `Features/<Slice>/ServiceCollectionExtensions.cs` with `Add<Slice>()`: `AddProxiedHttpClient<<Interface>Client>(environment, "admin-api")`,
   the view models `AddScoped`, form validators as `IValidator<Form>`. `Program.cs` of the Web project calls it.
2. Pages with `public const string Path` / `PathOf(id)`, `@attribute [RequiresPermission(…Permissions.Names.…)]`,
   view models on `ViewModelPage<T>` with `AsyncCommand` loads and `ToOutcome()`, a `data-testid` on everything a test
   touches.
3. A form: `Form`, `FormValidator` applying the generated `{Model}Rules`, `Mapper`, `<AppForm>`.
4. The menu entry in `Foundation/Shell/MainLayout.razor`, shown with `context.User.Has(<Permission>)`, its text in
   `MainLayout.resx`.
5. Every text in the component's resx pair (`@inject I<Component>Localizations L`). A new contract's field names and
   pattern messages in German go into `Admin.Shared/<Model>.de.resx` (keys `<Member>`, `<Member>.pattern`).

New permissions appear in the group editor by themselves, labelled from their names. Their German labels go into
`PermissionLabels.de.resx`, keyed by slice and by permission.

## 6. Tests

- **Integration** (`tests/FocusTemplate.Admin.Api.IntegrationTests`, through the generated client): per operation the
  answer, the 404, each 409, the 400, and the 403 without the permission.
- **Unit**: a view model against a fake HTTP handler (`ListPageViewModelTests`), the form's validator.
- **E2E**: one flow through the BFF on `BffPageTest`, selected by `data-testid`, including the menu entry.
- `AuthenticationTests.EveryApiEndpointRequiresAuthorization` covers the new endpoints by itself.

## 7. Finish

1. `dotnet format` on the touched projects, `dotnet build FocusTemplate.slnx`, the tests (AGENTS.md, **Build, test,
   format**).
2. A statement in AGENTS.md, `docs/` or `.claude/rules/` the slice made wrong is corrected, and a new pattern goes into
   its doc.
3. One commit holds the spec, the generated code, `openapi.yaml` and the migration.
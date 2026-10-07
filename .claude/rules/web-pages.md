---
paths:
  - "src/admin/*.Admin.Web/Features/**"
  - "src/admin/*.Admin.Web/Foundation/{Pages,Feedback,Shell}/**"
  - "src/admin/*.Admin.Web/Foundation/*.cs"
  - "tests/*.Admin.Web.UnitTests/Features/**"
---
# Web pages

- A `.razor` file opens with its directives in one block without blank lines: route, `@layout`, other `@attribute`s,
  `@using`, `@typeparam`, `@inherits`, `@implements`, `@inject`.
- A slice's files are named without the slice's prefix (`Form`, `Mapper`). Form, validator and mapper are `internal`.
  Pages, view models and the registration class are `public`. The slice registers its services by hand in
  `Add<Slice>()`.
- The view model holds the page's state and server calls. The page keeps markup, confirmations, dialogs, navigation and
  permission checks.
- A page `@inherits ViewModelPage<TViewModel>`. Its view model is registered `AddScoped` and takes nothing scoped from the
  app, such as `DialogService` or `AuthenticationStateProvider`.
- A load is an `AsyncCommand<T>` with `ReplaceRunning` through `.WithBusy(busyState, token)`, an action one with
  `IgnoreWhileRunning`, its button disabled through `IsRunning`. A private fetch method behind a command has a name of its
  own (`FetchAsync`).
- A call's result becomes an outcome through `ToOutcome()` or `ToOutcome<TProblem>()`, and no view model catches.
  `Failure = outcome is ApiFailure failure ? failure.Message : null;`.
- Failures show through `<AppFailure>`, a first load through `<AppLoading>`, a detail page's states through
  `<AppItemView TItem="…">`, a grid's reload through its `IsLoading` mask.
- Services a slice's pages share come from `@inject` in the slice's `_Imports.razor`.
- A list page declares `public const string Path`, a detail page `PathOf(id)`, and routes, links, `NavigateTo` and the menu
  build on them.

Background: [docs/web-pages.md](../../docs/web-pages.md).
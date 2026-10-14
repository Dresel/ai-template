---
paths:
  - "src/admin/*.Admin.Web/Features/**"
  - "src/features/*/*.Web/**"
  - "src/admin/user-management/*.Web/**"
  - "src/*.Web.Foundation/{Pages,Feedback,Appearance}/**"
  - "src/admin/*.Admin.Web/wwwroot/**"
  - "src/admin/*.Admin.Web/Shell/**"
  - "src/*.Web.Hosting/**"
  - "src/*.Web.Foundation/*.cs"
  - "tests/*.Admin.Web.UnitTests/Features/**"
  - "tests/*.Web.UnitTests/{UserManagement,Weather}/**"
---
# Web pages

- A `.razor` file opens with its directives in one block without blank lines: route, `@layout`, other `@attribute`s,
  `@using`, `@typeparam`, `@inherits`, `@implements`, `@inject`.
- A slice's files are named without the slice's prefix (`Form`, `Mapper`). Form, validator and mapper are `internal`.
  Pages, view models and the registration class are `public`. The slice registers its services by hand in
  `Add<Slice>()`.
- A feature's `Add<Feature>Pages()` registers its `PageAssembly`, which `AppRouter` hands to the `Router`. The host
  names no feature assembly itself.
- The view model holds the page's state and server calls. The page keeps markup, confirmations, dialogs, navigation and
  permission checks.
- A page `@inherits ViewModelPage<TViewModel>`. Its view model is registered `AddScoped` and takes nothing scoped from the
  app, such as `DialogService` or `AuthenticationStateProvider`.
- A load is an `AsyncCommand<T>` with `ReplaceRunning` through `.WithBusy(busyState, cancellationToken)`, an action one with
  `IgnoreWhileRunning`, its button disabled through `IsRunning`. A private fetch method behind a command has a name of its
  own (`FetchAsync`).
- A call's result becomes an outcome through `ToOutcome()` or `ToOutcome<TProblem>()`, and no view model catches.
  `Failure = outcome is ApiFailure failure ? messages.Of(failure) : null;`, with `ApiFailureMessages` injected.
- Failures show through `<AppFailure>`, a first load through `<AppLoading>`, a detail page's states through
  `<AppItemView TItem="…">`, a grid's reload through its `IsLoading` mask.
- Text a user reads comes from the component's resx through `@inject I{Component}Localizations L`.
- Services a slice's pages share come from `@inject` in the slice's `_Imports.razor`.
- A page that stays current has its view model listen on a `LiveUpdates<T>`, one per view model, which the slice
  registers with
  `services.AddLiveUpdates((GroupsClient groups, CancellationToken cancellationToken) => groups.WatchAsync(cancellationToken))`.
- A list page declares `public const string Path`, a detail page `PathOf(id)`, and routes, links, `NavigateTo` and the menu
  build on them.
- Styles that differ between light and dark key on `html[data-theme="dark"]`, which `appearance.js` sets before the
  first paint. The theme changes through `AppearanceStorage` and Radzen's `ThemeService` alone.

Background: [docs/web-pages.md](../../docs/web-pages.md).
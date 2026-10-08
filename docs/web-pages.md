# Web pages

What to follow when changing these files: [the rule](../.claude/rules/web-pages.md).

## The project

`FocusTemplate.Admin.Web` is the Blazor WebAssembly client on Radzen (`software` theme, `software-dark` behind the
header's appearance toggle, Material Symbols bundled). It talks to the API through the generated clients of
`FocusTemplate.Admin.Client`, and learns about the signed-in user from the BFF (`BffAuthenticationStateProvider`).
`RadzenTheme` in `App.razor` loads the theme through `HeadContent`, `<RadzenComponents />` hosts notifications and
dialogs, and `AddRadzenComponents()` registers their services.

- **`Features/<Slice>/`**: a slice's pages, view models, form, validator, mapper and its registration
  (`Add<Slice>()`), named without the slice's prefix (`Form`, `FormValidator`, `Mapper`). The form, validator and
  mapper are `internal`. Pages, view models and the registration class are `public`: Razor generates public pages, and a
  public page cannot derive from a base with an internal type argument. A slice with several areas gets a folder per
  area (`Features/UserManagement/Users/`, `Groups/`) with its registration and an `_Imports.razor` one level up, which
  imports the slice's contracts (`FocusTemplate.Admin.Shared.UserManagement`). The validation demo is
  `Features/DemoProfiles/` at `/demo/validation`.
- **`Foundation/`**, domain-neutral:
  - `Validation/Core/` (`FormMessages` with its field registrations, `FormValidation`, `ValidatorBase`) and
    `Validation/App/` (`FormHostBase`, `AppInputBase`, `IFormHost`), see [web forms](web-forms.md)
  - `Forms/`: the Radzen wrappers (`AppForm`, the inputs, `AppValidationMessage`)
  - `Pages/`: `ViewModelPage<TViewModel>`, `IViewModel`
  - `Feedback/`: `AppFailure`, `AppItemView`, `AppLoading`, `DialogService.ConfirmAsync`, `ApiFailureMessages`
  - `Shell/`: `MainLayout`, `Home`, `NotFound`, `Forbidden`
  - `Diagnostics/`: the request diagnostics page
  - directly in it, the async helpers a view model composes: `Debouncer` (one per input), `AsyncCommand<T>`
    (`IgnoreWhileRunning` for a button, `ReplaceRunning` for a list's loads, where a replaced run's cancellation ends quietly)
    and `BusyState` through `task.WithBusy(busyState, cancellationToken)` (a mask only after a delay and then for a minimum, with a stop
    that throws for a replaced run, so nothing it fetched is shown). `AddFoundation()` registers `Debouncer` and `BusyState`
    transient with their timings, so a view model takes them in its constructor and the page's scope disposes them. A
    view model disposes only the commands it creates. They run on the renderer's dispatcher and take no locks.
    `Users/ListPageViewModel` uses all of them.
- **`Infrastructure/`**: `Authentication/`, `Authorization/`, `Configuration/`, `Localization/` (the culture's storage,
  its setting at startup and the header's `LanguageSwitch`, see [localization](localization.md)).
- **Texts** a page shows come from its resx pair through `@inject I{Page}Localizations L`, as do a confirmation's
  message and title. Radzen's own texts, such as a confirmation's cancel button, come in its own German.
- `Riok.Mapperly` maps between a form's view model and the contract.

## Pages and view models

As the user management pages (`Features/UserManagement/`) do it.

- **Directives** open a `.razor` file in one block without blank lines, which ReSharper's formatter would otherwise
  remove: the route (`@page` or `@attribute [Route(…)]`), `@layout`, the other `@attribute`s, `@using`, `@typeparam`,
  `@inherits`, `@implements`, `@inject`.
- **Split**: the view model (`ListPageViewModel`, `DetailPageViewModel`, next to its page) holds the page's state and
  its server calls. The page keeps the markup, the confirmations, the dialogs, the navigation and the permission checks
  (properties over the injected `CurrentUser` named after the permission they check, `CanManageGroups` and
  `CanViewUsers`, plus `Self`). A confirmation asks in the page and calls the view model's action on a yes, so the view
  model stays free of Radzen's UI services and its tests need no fake dialogs.
- **Lifetime**: `@inherits ViewModelPage<TViewModel>` resolves the view model from a DI scope of the page's own
  (`OwningComponentBase`), disposed with the page, and renders on `IViewModel.Changed`, for the changes outside an event
  handler: the delayed busy state, and a reload a `ChangeFeed<T>` started (the groups list). The slice registers its view
  models `AddScoped`. A view model takes nothing scoped
  from the app (`DialogService`, `AuthenticationStateProvider`): the page's scope would hand it a second, unconnected
  instance.
- **One page per address**: Blazor would reuse a page when only its route values change (`/groups/A` → `/groups/B`),
  so `MainLayout` keys `@Body` by the path (without the query, which a page may keep its filters in). Another id is a
  new page with a new view model and new child components, and the old one is disposed with its running calls. So a
  page loads once, `OnInitializedAsync` passing its parameters to `LoadAsync(id)`.
- **Loads and actions**: a load is an `AsyncCommand<T>` with `ReplaceRunning` (another page or a reload replaces the
  one still loading) through `.WithBusy(busyState, cancellationToken)`. An action is one with `IgnoreWhileRunning`, its button
  `Disabled` through `IsRunning`. A replaced load's answer never lands, so a view model shows it without checking.
  Loading shows only while the busy state is on, so a quick answer never flashes it.
- **Loading on screen**: before the first answer it is `<AppLoading Busy="…">`, a thin indeterminate bar in a slot that
  keeps its height either way, so nothing below it moves. Once there is data, a reload keeps it on screen under the
  grid's own `IsLoading` mask (`ViewModel.Loading && ViewModel.Users is not null`). A list's grid stands from the start,
  headers and all (one that loads its pages through `LoadData` must, since it asks for the first itself), with its
  `EmptyText` blank until the first answer, so it never claims there are no rows while they load.
- **Fetch method names**: a private fetch method behind a command takes a name of its own (`FetchAsync`): next to a
  public `LoadAsync(id)` of the same name, CA2016 would demand the token on every reload and bypass the command.
- **Feedback**: `<AppFailure Message="@ViewModel.Failure" data-testid="…" />` renders nothing without a message.
  `await Dialogs.ConfirmAsync(message, title, confirm)` is true only for a yes.
- **A detail page's states** go through `<AppItemView TItem="…" Item="…" Loading="…" Missing="…" Context="group">`, in
  the order they happen: the loading bar while there is no item and the busy state is on, the not-found title with a
  link back, or the item. It also sets the tab's title. A reload keeps the item on screen (`ReloadingContent` may add to
  it, `LoadingContent` replaces the bar). `TItem` is named, since inferred from the nullable `Item` it would make the
  content's item nullable too.
- **Shared services** come from `@inject` in the slice's `_Imports.razor` (`DialogService`, `NavigationManager`). A base
  class would hide them and take the one `@inherits` a page has.
- **Routes**: a list page declares `public const string Path`, a detail page `PathOf(id)` built on it, and both route
  through `@attribute [Route("/" + ListPage.Path + …)]`, so matching and linking share one string. Links, `NavigateTo`
  and the menu build on these.

## Answers

A view model turns a call's result union into an outcome:

- `ToOutcome()` gives an `ApiOutcome<T>`: the answer or an `ApiFailure`. An action uses it, and so does an operation
  that models no problem and answers without a union (`ListAsync()`).
- `ToOutcome<TProblem>()` gives an `ApiOutcome<T, TProblem>`, which passes the one problem the caller handles itself as
  the client's own case record: a load `.ToOutcome<NotFoundProblem>()` for its missing state (`case GroupResponse` /
  `case NotFoundProblem` / `case ApiFailure`), a form `.ToOutcome<ValidationProblem>()` (see [web forms](web-forms.md)).
  Each problem record implements `IProblemOf<TUnion>` for the unions it appears in, so a problem the operation cannot
  answer does not compile (CS0311).
- Every other problem becomes an `ApiFailure(Status, Problem)`, and so does an `HttpRequestException` (the client throws
  it for a status the contract does not model and for no answer) and a call `HttpClient.Timeout` ends. No view model
  catches, and what retrying cannot fix (an unreadable body, a bug) stays an exception.
- Then `Failure = outcome is ApiFailure failure ? messages.Of(failure) : null;`, which says on its line that success
  clears it, and `if (outcome is Success)`. The view model takes `ApiFailureMessages messages` in its constructor.

The emitter generates the outcome types and methods next to each union in `Admin.Client`, describing the contract only.
The app's policy stays in the app: `ApiFailureMessages` (the server's words, or the status, or "not answering") in
`Foundation/Feedback/`, and `NoAnswerHandler` in `ClientServiceDefaults`, outside the resilience handler, which turns
Polly giving up (`ExecutionRejectedException`) into an `HttpRequestException` without a status, so the client and its
outcomes stay free of Polly. C# matches a union's direct cases only, so the outcome cannot wrap the client's union.
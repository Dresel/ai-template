# Web forms

What to follow when changing these files: [the rule](../.claude/rules/web-forms.md).

How a form validates, as the demo at `/demo/validation` (`Features/DemoProfiles/`) shows it in full. The group form
(`Features/UserManagement/Groups/FormDialog.razor`) is the everyday case.

## View model and rules

- **View model**: a mutable class shaped for the form (`int?` so an input can be empty, a row object per list item,
  keyed in the markup), plain data, never the contract itself. A Mapperly `[Mapper]` converts: `[MapProperty]` for a
  renamed member, and user conversions such as `string Sent(string? value) => value!`, since Mapperly throws on a null
  required member while the server, validating again, is the one to report it.
- **Validator**: derives from `ValidatorBase<TModel>` and applies the generated `{Model}Rules` to the view model's own
  members. It is registered as `IValidator<TModel>` in the slice's `Add<Slice>()` and injected by the page
  (`@inject IValidator<Form> FormValidator`), so DI supplies what its rules need. The generated rules carry the
  contract's display names, translated through the contract's resx (see [localization](localization.md)), so a member
  the view model names differently still reads like the contract's.
- **Rules the spec cannot state** go into a hand-written class in `Admin.Shared` (`DemoProfileCustomRules`), which the
  API's validator and the view model's both apply, so code and message are written once.
- **Wire keys** are never written as strings: the API's validator and a handler's own failures key with the generated
  paths (`AdminPaths.DemoProfileRequest.Nickname`), so a renamed or moved member breaks the build.
- **Dependent fields**: a rule that reads another field declares it beneath the rule,
  `DependsOn(form => form.MaxTemperatureC, on: form => form.MinTemperatureC)`, so a change to the lowest validates the
  highest too.
- **Server checks**: what only the server knows (a code already taken) is an async shared rule that takes its lookup:
  the API's validator passes the store, the view model's a GET per value in the spec (`GET /demo-profiles/codes/{code}`,
  safe to repeat, nothing sensitive in the URL), last in its chain behind `Cascade(CascadeMode.Stop)` so it asks only
  about a value that passes its own rules. The API's validator checks again on submit.

## When the rules run

- A change validates its field and the fields depending on it (a list row's input its whole list, a field no input
  claims the whole form), at once, or once the server answers where an async rule asks it.
- A run takes the place of every waiting run it shares a field with and validates their fields along with its own, so
  no answer for an older value lands. The whole form shares a field with every run.
- A submit validates the whole form and waits for the async rules. A submit whose place a change or a second submit
  took does nothing, so the form is sent once and never with a value it did not check.
- A field shows its messages once it changed or lost focus, and every field once a submit was tried.
- Nothing waits for the typing to pause. A debounce on a field's input, releasing its value on blur and before submit,
  is designed for checks that need one but not built yet.

## Markup

- `<AppForm Model="form" Validator="…" Renames="…Mapper.Renames" OnValidSubmit="…">` with an `AppTextInput` or
  `AppNumericInput` per member (`@bind-Value`, `Label`, `data-testid`). `AppForm` builds the form's messages and
  validation from the model and its validator and keeps them while it is rendered, and a new model is a new form.
- A member the view model names differently from the wire gets an entry in the mapper's `Renames` (the wire path,
  `AdminPaths.DemoProfileRequest.Name`, to the view model's), next to its `[MapProperty]`.
- A list row's input names its path (`Path="Tags[1]"`). A member no input edits shows its messages through
  `AppFieldMessages`, and whatever no rendered input claims shows in the summary `AppForm` renders.
- A value changed in code, not through an input, calls `appForm.NotifyFieldChangedAsync(field)`.
- **Messages at a field**: one line, the most severe with its icon, and a pill counting the rest. Hovering the line
  lists them all, clicking the pill opens them grouped by severity, as the summary groups its own. The store only says
  which messages a field has (`For(field)`). `Foundation/Forms/` decides how they show, and the input takes its state
  from the most severe.

## Submit

Call the client, turn its union into an `ApiOutcome<T, ValidationProblem>` with `.ToOutcome<ValidationProblem>()` (see
**Answers** in [web pages](web-pages.md)) and switch over its three cases: the answer (a success clears the server's
messages), the `ValidationProblem` for `appForm.ShowServerErrorsAsync(invalid.Problem)`, which puts the errors at their
inputs and focuses the first, and an `ApiFailure`, whose message is an alert above the fields. Create and update share
one outcome, so a form for both needs one switch.

## Radzen

Radzen styles a field's state only through an `EditContext`, which these forms do not use. So each wrapper puts the
state on the input element itself (`aria-invalid` for an error, `data-severity` for `app.css`, `aria-describedby` for
the line below) and renders its messages through `AppValidationMessage`, the tooltip and the dialog through Radzen's
`TooltipService` and `DialogService`. `RadzenTextBox` is a bare `<input>` that takes the attributes directly.
`RadzenNumeric` wraps its input in a span, so its attributes go to `InputAttributes`. Both run with `Immediate` (a value
on every keystroke) and focus with `FocusAsync()`.

Another UI library rewrites `Foundation/Forms/`: its form derives from `FormHostBase<TModel>`, which owns the form's
messages and validation, the submit and the `IFormHost` its inputs call, and overrides `RefreshAsync` only where the
library keeps a validation state of its own to trigger again. Its inputs derive from `AppInputBase`. No folder is named
after the library (`Radzen`, `MudBlazor`): that namespace segment would hide the library's own namespace from the Razor
files inside it.

## Unit tests

`FocusTemplate.Admin.Web.UnitTests` runs without a browser and without a server: the forms store and validation
(`FormMessages`, `FormValidation`), the demo form's validator and mapping, the Foundation helpers (`AsyncCommand`,
`BusyState`, `Debouncer`), view models against a fake HTTP handler (`ListPageViewModelTests`), and components rendered
with `HtmlRenderer` over a small service provider (`PermissionViewTests`, `RequiresPermissionTests`). The emitter's own
tests live in its repository.
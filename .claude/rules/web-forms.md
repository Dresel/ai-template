---
paths:
  - "src/admin/*.Admin.Web/Foundation/{Forms,Validation}/**"
  - "src/admin/*.Admin.Web/Features/**/{Form,FormValidator,Mapper}*.cs"
  - "src/admin/*.Admin.Shared/*Rules.cs"
  - "tests/*.Admin.Web.UnitTests/Foundation/**"
---
# Web forms

- A form edits a mutable view model shaped for it. A Mapperly `[Mapper]` converts it to and from the contract, with
  `[MapProperty]` for a renamed member and its entry in the mapper's `Renames`.
- The view model's validator derives from `ValidatorBase<TModel>`, applies the generated `{Model}Rules` and is
  registered as `IValidator<TModel>` in `Add<Slice>()`.
- A rule the spec cannot state goes into a class in `Admin.Shared`, which the API's validator and the view model's both
  apply.
- Keys come from the generated paths (`AdminPaths.…`).
- A rule that reads another field declares it with `DependsOn(…)`.
- A check only the server can answer is an async shared rule, last in its chain behind `Cascade(CascadeMode.Stop)`.
- Markup: `<AppForm Model Validator Renames OnValidSubmit>` with one `AppTextInput` or `AppNumericInput` per member. A
  list row's input names its `Path`, and a value changed in code calls `appForm.NotifyFieldChangedAsync(field)`.
- Submit with `.ToOutcome<ValidationProblem>()` and switch over the answer, `appForm.ShowServerErrorsAsync(…)` for the
  validation problem, and an alert for an `ApiFailure`.
- A wrapper puts the field's state on the input element (`aria-invalid`, `data-severity`, `aria-describedby`).
- Another UI library rewrites `Foundation/Forms/` on `FormHostBase` and `AppInputBase`, in a folder not named after the
  library.

Background: [docs/web-forms.md](../../docs/web-forms.md).
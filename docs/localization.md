# Localization

English is the default language, German the second. The mechanism is Lama's (resx files with typed classes from
ResXGenerator, a custom FluentValidation language manager, the culture from local storage), extended to the API, which
answers in the language the app shows.

Packages: `Aigamo.ResXGenerator` (MIT, a source generator over resx files), for now a local build from `.nuget/` that
combines [ycanardeau/ResXGenerator#15](https://github.com/ycanardeau/ResXGenerator/pull/15)
(`ResXGenerator_InternalRegistration`) and
[ycanardeau/ResXGenerator#13](https://github.com/ycanardeau/ResXGenerator/pull/13) (`GenerateFormatMethods`), and
`Microsoft.Extensions.Localization`.
FluentValidation and Radzen ship their own German texts.

## Cultures

- `Cultures` in `Admin.Shared/Localization/` lists what the app speaks: `Default` (en) and `Supported` (en, de).
  `ResolveOrDefault` keeps a supported culture as it is, so `de-AT` keeps its formats, and turns any other into
  `Default`. A fork with another default language changes `Default`.
- **Web**: `CultureStorage` reads the user's choice from local storage (key `culture`), and without one the browser's
  language. `UseStoredCultureAsync()` sets it as the default thread culture and as the page's `lang` before
  `RunAsync`, which loads the satellite assemblies of the culture set by then. The header's `LanguageSwitch` stores a
  choice and reloads, since the culture is set once per load.
- **API**: request localization from `Accept-Language` alone, with the cultures of `Cultures`. Proxied clients send the
  app's culture through `AcceptLanguageHandler` (`AddProxiedHttpClient`), since the browser's own header does not know
  the user's choice. YARP forwards it unchanged.
- **Keycloak**: `NavigateToLogin()` (`ClientServiceDefaults`) passes the culture to `/bff/login?culture=`. The BFF keeps
  it only when `Cultures` supports it and sends its two-letter language as `ui_locales` inside the pushed authorization
  request. The local realm has internationalization on (`supportedLocales` en, de). An external realm needs the same.

## Resources

- One resx per component or class, next to it: English in the neutral file (`MainLayout.resx`), German beside it
  (`MainLayout.de.resx`, nested in the IDE). Keys name the text (`LogIn`, `ConfirmDelete`). A text with values takes
  `{0}`, and `{0:g}` where a date needs its format.
- **One mode everywhere**, as in Lama: ResXGenerator's `StringLocalizer` mode generates `I{Name}Localizations` and a
  class behind it from the neutral resx at build time, one property per key, registered as singletons. The generated
  code is not in the repository, `-p:EmitCompilerGeneratedFiles=true` writes it under `obj/` to look at. Each reads
  its resx through `IStringLocalizer` in the culture of the moment, the request's on the server.
  `ConventionBasedStringLocalizerFactory` (`Admin.Shared`) maps `MainLayoutLocalizations` to `MainLayout.resx`.
- **Texts with values** become methods (`GenerateFormatMethods` in the project files), one `object` parameter per
  index of the neutral text: `localizations.NoGroup(command.Id)`, `L.Seen(first, last)`. The localizer formats them in
  the culture of the moment. A translation may leave a value out, and an index the neutral text lacks throws a
  `FormatException` when the text is read.
- **Registration**: each host calls its own `UsingResXGenerator()` and `AddAdminSharedLocalization()`, which registers
  the localization services, that factory and the texts of `Admin.Shared`. ResXGenerator gives every assembly a
  registration class of the same name, and two visible ones make `UsingResXGenerator()` ambiguous, so `Admin.Shared`
  generates its own `internal` (`ResXGenerator_InternalRegistration`, from the local package of
  [ycanardeau/ResXGenerator#15](https://github.com/ycanardeau/ResXGenerator/pull/15)).
- **Who reads how**: a component injects `@inject IMainLayoutLocalizations L`, a service or view model takes its texts
  in its constructor (`ApiFailureMessages`). In the API one resx per interface folder, named after it
  (`Features/UserManagement/Groups/Groups.resx`), serves its handlers and validators through
  `IGroupsLocalizations localizations`. A shared rule takes its texts as a parameter, as it takes a lookup
  (`DemoProfileCustomRules.CodeNotTaken(rule, isTaken, localizations)`), and both validators pass them in.
- **Read by key**: a resx whose keys are data (permissions) or follow a type found at run time (contracts) is read
  through `IStringLocalizer` and gets no generated class, `SkipFile` in its project file. ResXGenerator's
  `StringLocalizer` mode would generate an invalid property for a dotted key such as `Code.pattern`.
- **Permissions** are labelled by `PermissionLabels` from `Features/UserManagement/PermissionLabels.resx`, keyed by slice
  (`UserManagement`) and by permission (`UserManagement.ViewUsers`). A permission the resx lacks reads off its name.
- **Radzen** shows its own German for its built-in texts (pager, dialog buttons, empty grid). To word one differently,
  an app resource named `Radzen.Blazor.RadzenStrings` (`LogicalName` in the project file) with Radzen's key overrides
  it, since Radzen looks up the entry assembly's resources before its own.
- Untranslated: data (group names, forecast summaries, the seeded `Administrators`), logs, exception messages, and the
  header names on the Diagnostics page.

## Validation messages

A message has a sentence and a field name in it, `'Kürzel' darf nicht leer sein.`

- **Sentences** of FluentValidation's built-in validators come in the UI culture. `CustomLanguageManager`
  (`Admin.Shared/Localization/`) adds what FluentValidation lacks from its resx through
  `IStringLocalizer<CustomLanguageManager>`, the item counts the generated rules ask
  for (`MinItemsValidator`, `MaxItemsValidator`), and can reword a built-in one. It looks up on every message, in the
  culture of the moment.
- **Field names and pattern messages** of the generated rules come from the emitter's `AdminTexts` hook.
  `ValidationTexts.UseResources()` (`Admin.Shared/Localization/`), which both hosts call at startup, answers from the
  resx named after the contract type in its namespace's folder: `DemoProfileRequest.de.resx` in `Admin.Shared`, keys
  `Code` and `Code.pattern`, and `UserManagement/GroupRequest.de.resx` for a feature's contract. The neutral file keeps
  the generated readable names, so English messages read as before.
- The hook and FluentValidation's language manager are process-wide and outlive a host, so they read through a localizer
  factory of their own, which no host disposes with its container. Integration tests start a host per test class in one
  process.
- **Shared rules** (`DemoProfileCustomRules`) read their messages per validation from the texts passed to them
  (`WithMessage(_ => localizations.CodeTaken)`). The API's validators and handlers read theirs from the interface's
  resx.
- The server translates by the request's culture, so a form shows the same words whether the browser or the server
  found the problem.

## Tests

- **Unit**: `UiCulture.Use("de")` sets a test's culture for texts and formats, as the app does, and puts the previous
  ones back. Tests that assert English texts pin English the same way, so no test depends on the machine's language.
  `Localizations.Get<T>()` hands a class a test builds by hand its texts, registered as the Web app registers them.
- **Integration**: an `Accept-Language` header on the test's client (`DemoProfileTests`, `UsersTests`).
- **E2E**: `GermanBrowserTests` (Playwright's `Locale`), `LanguageSwitchTests` (the English test browser switching), and
  `AuthenticationTests.KeycloaksFormSpeaksTheLanguageTheVisitorChose`. Tests select by `data-testid` and run in either
  language.

## Another language

Add it to `Cultures.Supported` and the realm's `supportedLocales`, and add its resx beside every neutral file. The
`.de.resx` patterns in the Web, API and Shared project files (`EmbeddedResource` nesting) need the new suffix too.
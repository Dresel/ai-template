using FluentValidation;

namespace FocusTemplate.Admin.Shared;

// Demo rules shared by API and UI. A server-dependent rule receives a lookup, every rule the localizations of its
// messages, which it asks on every validation so they follow the culture of the moment.
public static class DemoProfileCustomRules
{
	public static IRuleBuilderOptions<T, int> AgeUnderHundred<T>(
		IRuleBuilder<T, int> rule,
		IDemoProfileCustomRulesLocalizations localizations) =>
		rule.Must(age => age < 100).WithSeverity(Severity.Info).WithErrorCode("age.high").WithMessage(_ => localizations.AgeHigh);

	// The form's empty input binds to null
	public static IRuleBuilderOptions<T, int?> AgeUnderHundred<T>(
		IRuleBuilder<T, int?> rule,
		IDemoProfileCustomRulesLocalizations localizations) =>
		rule.Must(age => age is null or < 100)
			.WithSeverity(Severity.Info)
			.WithErrorCode("age.high")
			.WithMessage(_ => localizations.AgeHigh);

	// Oblivious like the generated rules, so one method takes the contract's string and the form's string?
#nullable disable
	public static IRuleBuilderOptions<T, string> CodeNotTaken<T>(
		IRuleBuilder<T, string> rule,
		Func<string, CancellationToken, Task<bool>> isTaken,
		IDemoProfileCustomRulesLocalizations localizations) =>
		rule.MustAsync(async (code, cancellationToken) => !await isTaken(code, cancellationToken))
			.WithErrorCode("code.taken")
			.WithMessage(_ => localizations.CodeTaken);
#nullable enable

	public static IRuleBuilderOptions<T, int?> MaxTemperatureNotBelowMin<T>(
		IRuleBuilder<T, int?> rule,
		Func<T, int?> min,
		IDemoProfileCustomRulesLocalizations localizations) =>
		rule.Must((instance, max) => max is null || min(instance) is not { } lowest || max >= lowest)
			.WithErrorCode("maxTemperatureC.belowMin")
			.WithMessage(_ => localizations.MaxTemperatureBelowMin);

	public static IRuleBuilderOptions<T, int?> MinTemperatureNotAboveMax<T>(
		IRuleBuilder<T, int?> rule,
		Func<T, int?> max,
		IDemoProfileCustomRulesLocalizations localizations) =>
		rule.Must((instance, min) => min is null || max(instance) is not { } highest || min <= highest)
			.WithErrorCode("minTemperatureC.aboveMax")
			.WithMessage(_ => localizations.MinTemperatureAboveMax);

	public static IRuleBuilderOptions<T, string?> NicknameLettersOnly<T>(
		IRuleBuilder<T, string?> rule,
		IDemoProfileCustomRulesLocalizations localizations) =>
		rule.Must(nickname => nickname is null || nickname.All(char.IsLetter))
			.WithSeverity(Severity.Info)
			.WithErrorCode("nickname.lettersOnly")
			.WithMessage(_ => localizations.NicknameLettersOnly);

	public static IRuleBuilderOptions<T, string?> NicknameNotShouting<T>(
		IRuleBuilder<T, string?> rule,
		IDemoProfileCustomRulesLocalizations localizations) =>
		rule.Must(nickname => nickname is null || !nickname.Any(char.IsUpper) || nickname.Any(char.IsLower))
			.WithSeverity(Severity.Warning)
			.WithErrorCode("nickname.shouting")
			.WithMessage(_ => localizations.NicknameShouting);

	public static IRuleBuilderOptions<T, string?> NicknameWithoutDigits<T>(
		IRuleBuilder<T, string?> rule,
		IDemoProfileCustomRulesLocalizations localizations) =>
		rule.Must(nickname => nickname is null || !nickname.Any(char.IsDigit))
			.WithErrorCode("nickname.digits")
			.WithMessage(_ => localizations.NicknameDigits);

	public static IRuleBuilderOptions<T, string?> NicknameWithoutSpaces<T>(
		IRuleBuilder<T, string?> rule,
		IDemoProfileCustomRulesLocalizations localizations) =>
		rule.Must(nickname => nickname is null || !nickname.Any(char.IsWhiteSpace))
			.WithErrorCode("nickname.spaces")
			.WithMessage(_ => localizations.NicknameSpaces);
}
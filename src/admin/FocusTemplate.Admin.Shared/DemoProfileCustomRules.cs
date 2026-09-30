using FluentValidation;

namespace FocusTemplate.Admin.Shared;

// Demo rules shared by API and UI, server-dependent rules receive a lookup
public static class DemoProfileCustomRules
{
	public static IRuleBuilderOptions<T, int> AgeUnderHundred<T>(IRuleBuilder<T, int> rule) =>
		rule.Must(age => age < 100)
			.WithSeverity(Severity.Info)
			.WithErrorCode("age.high")
			.WithMessage("Over a hundred: please check the age.");

	// The form's empty input binds to null
	public static IRuleBuilderOptions<T, int?> AgeUnderHundred<T>(IRuleBuilder<T, int?> rule) =>
		rule.Must(age => age is null or < 100)
			.WithSeverity(Severity.Info)
			.WithErrorCode("age.high")
			.WithMessage("Over a hundred: please check the age.");

	// Oblivious like the generated rules, so one method takes the contract's string and the form's string?
#nullable disable
	public static IRuleBuilderOptions<T, string> CodeNotTaken<T>(
		IRuleBuilder<T, string> rule,
		Func<string, CancellationToken, Task<bool>> isTaken) =>
		rule.MustAsync(async (code, cancellationToken) => !await isTaken(code, cancellationToken))
			.WithErrorCode("code.taken")
			.WithMessage("The code is taken.");
#nullable enable

	public static IRuleBuilderOptions<T, int?> MaxTemperatureNotBelowMin<T>(IRuleBuilder<T, int?> rule, Func<T, int?> min) =>
		rule.Must((instance, max) => max is null || min(instance) is not { } lowest || max >= lowest)
			.WithErrorCode("maxTemperatureC.belowMin")
			.WithMessage("The highest temperature must not be below the lowest.");

	public static IRuleBuilderOptions<T, int?> MinTemperatureNotAboveMax<T>(IRuleBuilder<T, int?> rule, Func<T, int?> max) =>
		rule.Must((instance, min) => min is null || max(instance) is not { } highest || min <= highest)
			.WithErrorCode("minTemperatureC.aboveMax")
			.WithMessage("The lowest temperature must not be above the highest.");

	public static IRuleBuilderOptions<T, string?> NicknameLettersOnly<T>(IRuleBuilder<T, string?> rule) =>
		rule.Must(nickname => nickname is null || nickname.All(char.IsLetter))
			.WithSeverity(Severity.Info)
			.WithErrorCode("nickname.lettersOnly")
			.WithMessage("Letters only keep a nickname easy to type.");

	public static IRuleBuilderOptions<T, string?> NicknameNotShouting<T>(IRuleBuilder<T, string?> rule) =>
		rule.Must(nickname => nickname is null || !nickname.Any(char.IsUpper) || nickname.Any(char.IsLower))
			.WithSeverity(Severity.Warning)
			.WithErrorCode("nickname.shouting")
			.WithMessage("The nickname is all upper case.");

	public static IRuleBuilderOptions<T, string?> NicknameWithoutDigits<T>(IRuleBuilder<T, string?> rule) =>
		rule.Must(nickname => nickname is null || !nickname.Any(char.IsDigit))
			.WithErrorCode("nickname.digits")
			.WithMessage("The nickname must not contain digits.");

	public static IRuleBuilderOptions<T, string?> NicknameWithoutSpaces<T>(IRuleBuilder<T, string?> rule) =>
		rule.Must(nickname => nickname is null || !nickname.Any(char.IsWhiteSpace))
			.WithErrorCode("nickname.spaces")
			.WithMessage("The nickname must not contain spaces.");
}
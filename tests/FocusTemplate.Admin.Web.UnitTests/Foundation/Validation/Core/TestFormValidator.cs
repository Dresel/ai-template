using FluentValidation;
using FocusTemplate.Admin.Web.Foundation.Validation.Core;

namespace FocusTemplate.Admin.Web.UnitTests.Foundation.Validation.Core;

public sealed class TestFormValidator : ValidatorBase<TestForm>
{
	// With isTaken, the display name is asked about on the server
	public TestFormValidator(Func<string, CancellationToken, Task<bool>>? isTaken = null)
	{
		RuleFor(form => form.Name).NotNull().WithErrorCode("required").MaximumLength(5).WithErrorCode("maxLength");

		RuleFor(form => form.Nickname)
			.MaximumLength(8)
			.WithErrorCode("maxLength")
			.Must(nickname => nickname is null || !nickname.Any(char.IsUpper) || nickname.Any(char.IsLower))
			.WithErrorCode("shouting")
			.WithSeverity(Severity.Warning)
			.Must(nickname => nickname is null || !nickname.StartsWith("Ada", StringComparison.OrdinalIgnoreCase))
			.WithErrorCode("famous")
			.WithSeverity(Severity.Info);

		// Declared before the display name's own rules, so a run for the display name reads the nickname first
		RuleFor(form => form.Nickname)
			.Must((form, nickname) => nickname is null || nickname != form.DisplayName)
			.WithErrorCode("sameAsDisplayName");
		DependsOn(form => form.Nickname, form => form.DisplayName);

		RuleFor(form => form.Max)
			.Must((form, max) => max is null || form.Min is null || max >= form.Min)
			.WithErrorCode("belowMin");
		DependsOn(form => form.Max, form => form.Min);

		RuleFor(form => form.Address.Street).MaximumLength(5).WithErrorCode("maxLength");

		RuleForEach(form => form.Tags.Select(tag => tag.Value ?? string.Empty))
			.MinimumLength(1)
			.WithErrorCode("minLength")
			.OverridePropertyName("Tags");

		if (isTaken is not null)
		{
			RuleFor(form => form.DisplayName)
				.MustAsync(async (name, cancellationToken) => name is null || !await isTaken(name, cancellationToken))
				.WithErrorCode("taken");
		}
	}
}
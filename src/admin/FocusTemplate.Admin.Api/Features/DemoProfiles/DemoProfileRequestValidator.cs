using FluentValidation;
using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Api.Features.DemoProfiles;

public sealed class DemoProfileRequestValidator : AbstractValidator<DemoProfileRequest>
{
	public DemoProfileRequestValidator(
		DemoProfileRequestContractValidator contract,
		DemoProfileCodes codes,
		IDemoProfilesLocalizations localizations,
		IDemoProfileCustomRulesLocalizations ruleLocalizations)
	{
		Include(contract);
		DemoProfileRequestPath paths = AdminPaths.DemoProfileRequest;

		DemoProfileCustomRules.CodeNotTaken(RuleFor(profile => profile.Code), (code, _) => codes.IsTakenAsync(code), ruleLocalizations)
			.OverridePropertyName(paths.Code);

		DemoProfileCustomRules.NicknameNotShouting(RuleFor(profile => profile.Nickname), ruleLocalizations)
			.OverridePropertyName(paths.Nickname);

		DemoProfileCustomRules.NicknameWithoutDigits(RuleFor(profile => profile.Nickname), ruleLocalizations)
			.OverridePropertyName(paths.Nickname);

		DemoProfileCustomRules.NicknameWithoutSpaces(RuleFor(profile => profile.Nickname), ruleLocalizations)
			.OverridePropertyName(paths.Nickname);

		DemoProfileCustomRules.NicknameLettersOnly(RuleFor(profile => profile.Nickname), ruleLocalizations)
			.OverridePropertyName(paths.Nickname);

		DemoProfileCustomRules.AgeUnderHundred(RuleFor(profile => profile.Age), ruleLocalizations).OverridePropertyName(paths.Age);

		DemoProfileCustomRules.MaxTemperatureNotBelowMin(
				RuleFor(profile => profile.MaxTemperatureC),
				profile => profile.MinTemperatureC,
				ruleLocalizations)
			.OverridePropertyName(paths.MaxTemperatureC);

		DemoProfileCustomRules.MinTemperatureNotAboveMax(
				RuleFor(profile => profile.MinTemperatureC),
				profile => profile.MaxTemperatureC,
				ruleLocalizations)
			.OverridePropertyName(paths.MinTemperatureC);

		// Sample of a failure no input shows: keyed to the address as a whole, the form lists it in its summary
		RuleFor(profile => profile.Address)
			.Must(address => address is null || address.City != "Atlantis")
			.WithErrorCode("address.unknown")
			.WithMessage(_ => localizations.NoSuchAddress)
			.OverridePropertyName(paths.Address);
	}
}
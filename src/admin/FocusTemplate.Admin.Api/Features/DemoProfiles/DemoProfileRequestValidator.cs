using FluentValidation;
using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Api.Features.DemoProfiles;

public sealed class DemoProfileRequestValidator : AbstractValidator<DemoProfileRequest>
{
	public DemoProfileRequestValidator(DemoProfileRequestContractValidator contract, DemoProfileCodes codes)
	{
		Include(contract);
		DemoProfileRequestPath paths = AdminPaths.DemoProfileRequest;

		DemoProfileCustomRules.CodeNotTaken(RuleFor(profile => profile.Code), (code, _) => codes.IsTakenAsync(code))
			.OverridePropertyName(paths.Code);

		DemoProfileCustomRules.NicknameNotShouting(RuleFor(profile => profile.Nickname))
			.OverridePropertyName(paths.Nickname);

		DemoProfileCustomRules.NicknameWithoutDigits(RuleFor(profile => profile.Nickname))
			.OverridePropertyName(paths.Nickname);

		DemoProfileCustomRules.NicknameWithoutSpaces(RuleFor(profile => profile.Nickname))
			.OverridePropertyName(paths.Nickname);

		DemoProfileCustomRules.NicknameLettersOnly(RuleFor(profile => profile.Nickname))
			.OverridePropertyName(paths.Nickname);

		DemoProfileCustomRules.AgeUnderHundred(RuleFor(profile => profile.Age)).OverridePropertyName(paths.Age);

		DemoProfileCustomRules
			.MaxTemperatureNotBelowMin(RuleFor(profile => profile.MaxTemperatureC), profile => profile.MinTemperatureC)
			.OverridePropertyName(paths.MaxTemperatureC);

		DemoProfileCustomRules
			.MinTemperatureNotAboveMax(RuleFor(profile => profile.MinTemperatureC), profile => profile.MaxTemperatureC)
			.OverridePropertyName(paths.MinTemperatureC);

		// Sample of a failure no input shows: keyed to the address as a whole, the form lists it in its summary
		RuleFor(profile => profile.Address)
			.Must(address => address is null || address.City != "Atlantis")
			.WithErrorCode("address.unknown")
			.WithMessage("There is no such address.")
			.OverridePropertyName(paths.Address);
	}
}
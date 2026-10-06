using FluentValidation;
using FocusTemplate.Admin.Client.DemoProfiles;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Web.Foundation.Validation.Core;

namespace FocusTemplate.Admin.Web.Features.DemoProfiles;

internal sealed class FormValidator : ValidatorBase<Form>
{
	public FormValidator(DemoProfilesClient api)
	{
		When(
			form => form.ClientRules,
			() =>
			{
				// Asked only about a code that passes its own rules, which the cascade checks first
				DemoProfileCustomRules.CodeNotTaken(
					DemoProfileRequestRules.Code(RuleFor(form => form.Code).Cascade(CascadeMode.Stop)),
					(code, cancellationToken) => IsTakenAsync(api, code, cancellationToken));
				DemoProfileRequestRules.Name(RuleFor(form => form.DisplayName));
				DemoProfileRequestRules.Nickname(RuleFor(form => form.Nickname));
				DemoProfileCustomRules.NicknameNotShouting(RuleFor(form => form.Nickname));
				DemoProfileCustomRules.NicknameWithoutDigits(RuleFor(form => form.Nickname));
				DemoProfileCustomRules.NicknameWithoutSpaces(RuleFor(form => form.Nickname));
				DemoProfileCustomRules.NicknameLettersOnly(RuleFor(form => form.Nickname));
				DemoProfileRequestRules.Age(RuleFor(form => form.Age));
				DemoProfileCustomRules.AgeUnderHundred(RuleFor(form => form.Age));
				DemoProfileRequestRules.WeightKg(RuleFor(form => form.WeightKg));
				DemoProfileCustomRules.MaxTemperatureNotBelowMin(
					RuleFor(form => form.MaxTemperatureC),
					form => form.MinTemperatureC);
				DependsOn(form => form.MaxTemperatureC, form => form.MinTemperatureC);
				DemoProfileCustomRules.MinTemperatureNotAboveMax(
					RuleFor(form => form.MinTemperatureC),
					form => form.MaxTemperatureC);
				DependsOn(form => form.MinTemperatureC, form => form.MaxTemperatureC);

				DemoAddressRequestRules.Street(RuleFor(form => form.Address.Street));
				DemoAddressRequestRules.PostalCode(RuleFor(form => form.Address.PostalCode));
				DemoAddressRequestRules.City(RuleFor(form => form.Address.City));

				// The rows exist for the inputs. The rules see the strings the contract holds, at the contract's paths
				DemoProfileRequestRules
					.Tags(RuleFor(form => form.Tags.Select(tag => tag.Value ?? string.Empty).ToList()))
					.OverridePropertyName(nameof(Form.Tags));
				DemoProfileRequestRules
					.TagsItem(RuleForEach(form => form.Tags.Select(tag => tag.Value ?? string.Empty)))
					.OverridePropertyName(nameof(Form.Tags));
			});
	}

	private static async Task<bool> IsTakenAsync(
		DemoProfilesClient api,
		string code,
		CancellationToken cancellationToken)
	{
		try
		{
			return await api.CheckCodeAsync(code, cancellationToken) is DemoCodeResponse { Taken: true, };
		}
		catch (HttpRequestException)
		{
			return false;
		}
	}
}
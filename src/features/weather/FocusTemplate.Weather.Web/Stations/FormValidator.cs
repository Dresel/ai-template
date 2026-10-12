using FocusTemplate.Admin.Web.Foundation.Validation.Core;
using FocusTemplate.Weather.Shared;

namespace FocusTemplate.Weather.Web.Stations;

internal sealed class FormValidator : ValidatorBase<Form>
{
	public FormValidator()
	{
		StationRenameRequestRules.Name(RuleFor(form => form.Name));
	}
}
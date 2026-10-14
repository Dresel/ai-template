using SpatialFocus.Weather.Shared;
using SpatialFocus.Web.Foundation.Validation.Core;

namespace SpatialFocus.Weather.Web.Stations;

internal sealed class FormValidator : ValidatorBase<Form>
{
	public FormValidator()
	{
		StationRenameRequestRules.Name(RuleFor(form => form.Name));
	}
}
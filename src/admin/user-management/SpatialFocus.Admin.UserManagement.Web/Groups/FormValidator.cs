using SpatialFocus.Admin.UserManagement.Shared;
using SpatialFocus.Web.Foundation.Validation.Core;

namespace SpatialFocus.Admin.UserManagement.Web.Groups;

internal sealed class FormValidator : ValidatorBase<Form>
{
	public FormValidator()
	{
		GroupRequestRules.Name(RuleFor(form => form.Name));
		GroupRequestRules.Description(RuleFor(form => form.Description));
	}
}
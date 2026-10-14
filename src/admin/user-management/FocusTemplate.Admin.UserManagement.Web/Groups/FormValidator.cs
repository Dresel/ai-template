using FocusTemplate.Admin.UserManagement.Shared;
using FocusTemplate.Admin.Web.Foundation.Validation.Core;

namespace FocusTemplate.Admin.UserManagement.Web.Groups;

internal sealed class FormValidator : ValidatorBase<Form>
{
	public FormValidator()
	{
		GroupRequestRules.Name(RuleFor(form => form.Name));
		GroupRequestRules.Description(RuleFor(form => form.Description));
	}
}
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Admin.Web.Foundation.Validation.Core;

namespace FocusTemplate.Admin.Web.Features.UserManagement.Groups;

internal sealed class FormValidator : ValidatorBase<Form>
{
	public FormValidator()
	{
		GroupRequestRules.Name(RuleFor(form => form.Name));
		GroupRequestRules.Description(RuleFor(form => form.Description));
	}
}
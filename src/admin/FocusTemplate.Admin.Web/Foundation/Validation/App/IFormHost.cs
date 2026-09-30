using FocusTemplate.Admin.Web.Foundation.Validation.Core;
using Microsoft.AspNetCore.Components.Forms;

namespace FocusTemplate.Admin.Web.Foundation.Validation.App;

public interface IFormHost
{
	public FormMessages Messages { get; }

	public Task NotifyFieldChangedAsync(FieldIdentifier field);

	public Task NotifyFieldFocusLostAsync(FieldIdentifier field);

	public Task RefreshAsync();
}
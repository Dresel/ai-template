using Microsoft.AspNetCore.Components.Forms;
using SpatialFocus.Web.Foundation.Validation.Core;

namespace SpatialFocus.Web.Foundation.Validation.App;

public interface IFormHost
{
	public FormMessages Messages { get; }

	public Task NotifyFieldChangedAsync(FieldIdentifier field);

	public Task NotifyFieldFocusLostAsync(FieldIdentifier field);

	public Task RefreshAsync();
}
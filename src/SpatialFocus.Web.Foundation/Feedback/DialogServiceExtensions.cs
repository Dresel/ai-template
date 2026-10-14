using Radzen;

namespace SpatialFocus.Web.Foundation.Feedback;

public static class DialogServiceExtensions
{
	extension(DialogService dialogs)
	{
		public async Task<bool> ConfirmAsync(string message, string title, string confirm) =>
			await dialogs.Confirm(message, title, new ConfirmOptions { OkButtonText = confirm, }) is true;
	}
}
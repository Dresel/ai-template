using Radzen;

namespace FocusTemplate.Admin.Web.Foundation.Feedback;

internal static class DialogServiceExtensions
{
	extension(DialogService dialogs)
	{
		public async Task<bool> ConfirmAsync(string message, string title, string confirm) =>
			await dialogs.Confirm(
				message,
				title,
				new ConfirmOptions { OkButtonText = confirm, CancelButtonText = "Cancel", }) is true;
	}
}
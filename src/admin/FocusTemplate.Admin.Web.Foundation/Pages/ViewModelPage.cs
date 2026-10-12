using Microsoft.AspNetCore.Components;

namespace FocusTemplate.Admin.Web.Foundation.Pages;

public abstract class ViewModelPage<TViewModel> : OwningComponentBase<TViewModel>
	where TViewModel : class, IViewModel
{
	private bool subscribed;

	protected TViewModel ViewModel => Service;

	public override Task SetParametersAsync(ParameterView parameters)
	{
		if (!this.subscribed)
		{
			ViewModel.Changed += Refresh;
			this.subscribed = true;
		}

		return base.SetParametersAsync(parameters);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && this.subscribed)
		{
			ViewModel.Changed -= Refresh;
		}

		base.Dispose(disposing);
	}

	private void Refresh() => _ = InvokeAsync(StateHasChanged);
}
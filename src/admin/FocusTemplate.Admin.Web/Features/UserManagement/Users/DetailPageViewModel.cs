using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Client.UserManagement.Users;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Admin.Web.Foundation;
using FocusTemplate.Admin.Web.Foundation.Feedback;
using FocusTemplate.Admin.Web.Foundation.Pages;
using FocusTemplate.Primitives;

namespace FocusTemplate.Admin.Web.Features.UserManagement.Users;

public sealed class DetailPageViewModel : IViewModel, IDisposable
{
	private readonly UsersClient api;

	private readonly BusyState busyState;

	private readonly AsyncCommand<UserId> load;

	private readonly ApiFailureMessages messages;

	private readonly AsyncCommand<bool> setActive;

	private UserId id;

	public DetailPageViewModel(UsersClient api, BusyState busyState, ApiFailureMessages messages)
	{
		this.api = api;
		this.busyState = busyState;
		this.messages = messages;

		this.load = new AsyncCommand<UserId>(FetchAsync, AsyncCommandMode.ReplaceRunning);
		this.setActive = new AsyncCommand<bool>(SetActiveAsync, AsyncCommandMode.IgnoreWhileRunning);
	}

	public event Action? Changed
	{
		add => this.busyState.BusyChanged += value;
		remove => this.busyState.BusyChanged -= value;
	}

	public string? Failure { get; private set; }

	public bool Loading => this.busyState.IsBusy;

	public bool Missing { get; private set; }

	public bool SettingActive => this.setActive.IsRunning;

	public UserResponse? User { get; private set; }

	public Task ActivateAsync() => this.setActive.ExecuteAsync(true);

	public Task DeactivateAsync() => this.setActive.ExecuteAsync(false);

	public void Dispose()
	{
		this.load.Dispose();
		this.setActive.Dispose();
	}

	public Task LoadAsync(UserId id)
	{
		this.id = id;
		return this.load.ExecuteAsync(id);
	}

	private async Task FetchAsync(UserId id, CancellationToken cancellationToken)
	{
		Failure = null;

		switch (await this.api.GetAsync(id, cancellationToken).ToOutcome<NotFoundProblem>().WithBusy(this.busyState, cancellationToken))
		{
			case UserResponse found:
				User = found;
				Missing = false;
				break;

			case NotFoundProblem:
				User = null;
				Missing = true;
				break;

			case ApiFailure failure:
				Failure = this.messages.Of(failure);
				break;
		}
	}

	// Loaded again after the change, so the page shows what the server stored
	private async Task SetActiveAsync(bool active, CancellationToken cancellationToken)
	{
		ApiOutcome<Success> outcome = active
			? await this.api.ActivateAsync(this.id, cancellationToken).ToOutcome()
			: await this.api.DeactivateAsync(this.id, cancellationToken).ToOutcome();

		Failure = outcome is ApiFailure failure ? this.messages.Of(failure) : null;

		if (outcome is Success)
		{
			await LoadAsync(this.id);
		}
	}
}
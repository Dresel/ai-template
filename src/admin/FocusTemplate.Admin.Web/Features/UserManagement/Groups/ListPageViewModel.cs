using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Client.UserManagement.Groups;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Admin.Web.Foundation;
using FocusTemplate.Admin.Web.Foundation.Feedback;
using FocusTemplate.Admin.Web.Foundation.Pages;

namespace FocusTemplate.Admin.Web.Features.UserManagement.Groups;

public sealed class ListPageViewModel : IViewModel, IDisposable
{
	private readonly GroupsClient api;

	private readonly BusyState busyState;

	private readonly IDisposable listening;

	private readonly AsyncCommand<ValueTuple> load;

	private readonly ApiFailureMessages messages;

	private Action? reloaded;

	public ListPageViewModel(GroupsClient api, BusyState busyState, ChangeFeed<GroupChanged> changes, ApiFailureMessages messages)
	{
		this.api = api;
		this.busyState = busyState;
		this.messages = messages;

		this.load = new AsyncCommand<ValueTuple>((_, cancellationToken) => FetchAsync(cancellationToken), AsyncCommandMode.ReplaceRunning);

		// Any change can touch the list (a name, a member count, a group added or deleted), and so can a gap in the stream
		this.listening = changes.Listen(OnChanged, ReloadInBackground);
	}

	public event Action? Changed
	{
		add
		{
			this.busyState.BusyChanged += value;
			this.reloaded += value;
		}

		remove
		{
			this.busyState.BusyChanged -= value;
			this.reloaded -= value;
		}
	}

	public string? Failure { get; private set; }

	public IReadOnlyList<GroupSummaryResponse>? Groups { get; private set; }

	public bool Loading => this.busyState.IsBusy;

	public void Dispose()
	{
		this.listening.Dispose();
		this.load.Dispose();
	}

	public Task LoadAsync() => this.load.ExecuteAsync(default);

	private async Task FetchAsync(CancellationToken cancellationToken)
	{
		switch (await this.api.ListAsync(cancellationToken).ToOutcome().WithBusy(this.busyState, cancellationToken))
		{
			case IReadOnlyList<GroupSummaryResponse> groups:
				Groups = groups;
				break;

			case ApiFailure failure:
				Failure = this.messages.Of(failure);
				break;
		}
	}

	private void OnChanged(GroupChanged change) => ReloadInBackground();

	// A reload the stream started is no event the page handles, and a quick one never shows the busy state, so the view
	// model reports it itself
	private async Task ReloadAsync()
	{
		await this.load.ExecuteAsync(default);
		this.reloaded?.Invoke();
	}

	private void ReloadInBackground() => _ = ReloadAsync();
}
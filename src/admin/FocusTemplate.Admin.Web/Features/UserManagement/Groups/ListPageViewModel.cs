using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Client.UserManagement.Groups;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Admin.Web.Foundation;
using FocusTemplate.Admin.Web.Foundation.Feedback;
using FocusTemplate.Admin.Web.Foundation.Pages;
using FocusTemplate.Http;

namespace FocusTemplate.Admin.Web.Features.UserManagement.Groups;

public sealed class ListPageViewModel : IViewModel, IDisposable
{
	private readonly GroupsClient api;

	private readonly BusyState busyState;

	private readonly AsyncCommand<ValueTuple> load;

	private readonly ApiFailureMessages messages;

	private Action? fetched;

	public ListPageViewModel(GroupsClient api, BusyState busyState, LiveUpdates<GroupChanged> updates, ApiFailureMessages messages)
	{
		this.api = api;
		this.busyState = busyState;
		this.messages = messages;

		this.load = new AsyncCommand<ValueTuple>((_, cancellationToken) => FetchAsync(cancellationToken), AsyncCommandMode.ReplaceRunning);

		// Any change can touch the list (a name, a member count, a group added or deleted), and every connect reads it
		// again to cover what happened before the stream was there
		updates.Received += _ => Reload();
		updates.Connected += Reload;
	}

	public event Action? Changed
	{
		add
		{
			this.busyState.BusyChanged += value;
			this.fetched += value;
		}

		remove
		{
			this.busyState.BusyChanged -= value;
			this.fetched -= value;
		}
	}

	public string? Failure { get; private set; }

	public IReadOnlyList<GroupSummaryResponse>? Groups { get; private set; }

	public bool Loading => this.busyState.IsBusy;

	public void Dispose() => this.load.Dispose();

	public Task LoadAsync() => this.load.ExecuteAsync(default);

	private void Reload() => _ = this.load.ExecuteAsync(default);

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

		// A reload the stream started is no event the page handles, and a quick one shows no busy state, so the view
		// model reports every fetch itself
		this.fetched?.Invoke();
	}
}
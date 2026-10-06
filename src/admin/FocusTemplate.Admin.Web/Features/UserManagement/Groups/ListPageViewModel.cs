using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Client.UserManagement.Groups;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Admin.Web.Foundation;
using FocusTemplate.Admin.Web.Foundation.Feedback;
using FocusTemplate.Admin.Web.Foundation.Pages;

namespace FocusTemplate.Admin.Web.Features.UserManagement.Groups;

public sealed class ListPageViewModel(GroupsClient api, BusyState busyState) : IViewModel
{
	public event Action? Changed
	{
		add => busyState.BusyChanged += value;
		remove => busyState.BusyChanged -= value;
	}

	public string? Failure { get; private set; }

	public IReadOnlyList<GroupSummaryResponse>? Groups { get; private set; }

	public bool Loading => busyState.IsBusy;

	public async Task LoadAsync()
	{
		switch (await api.ListAsync().ToOutcome().WithBusy(busyState, CancellationToken.None))
		{
			case IReadOnlyList<GroupSummaryResponse> groups:
				Groups = groups;
				break;

			case ApiFailure failure:
				Failure = failure.Message;
				break;
		}
	}
}
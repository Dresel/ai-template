using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Client.UserManagement.Users;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Admin.Web.Foundation;
using FocusTemplate.Admin.Web.Foundation.Feedback;
using FocusTemplate.Admin.Web.Foundation.Pages;
using Radzen;

namespace FocusTemplate.Admin.Web.Features.UserManagement.Users;

public sealed class ListPageViewModel : IViewModel, IDisposable
{
	private readonly UsersClient api;

	private readonly BusyState busyState;

	private readonly AsyncCommand<LoadDataArgs> load;

	private readonly ApiFailureMessages messages;

	private readonly AsyncCommand<LoadDataArgs> reload;

	private readonly Debouncer searchDebouncer;

	private LoadDataArgs? lastPage;

	public ListPageViewModel(UsersClient api, BusyState busyState, Debouncer searchDebouncer, ApiFailureMessages messages)
	{
		this.api = api;
		this.busyState = busyState;
		this.searchDebouncer = searchDebouncer;
		this.messages = messages;

		this.load = new AsyncCommand<LoadDataArgs>(LoadAsync, AsyncCommandMode.ReplaceRunning);
		this.reload = new AsyncCommand<LoadDataArgs>((page, _) => this.load.ExecuteAsync(page), AsyncCommandMode.IgnoreWhileRunning);
	}

	public event Action? Changed
	{
		add => this.busyState.BusyChanged += value;
		remove => this.busyState.BusyChanged -= value;
	}

	public string? Failure { get; private set; }

	public bool Loading => this.busyState.IsBusy;

	public bool Reloading => this.reload.IsRunning;

	public string? Search { get; private set; }

	public int Total { get; private set; }

	public IReadOnlyList<UserSummaryResponse>? Users { get; private set; }

	public void Dispose()
	{
		this.load.Dispose();
		this.reload.Dispose();
	}

	public Task LoadAsync(LoadDataArgs args) => this.load.ExecuteAsync(args);

	public Task ReloadAsync() => this.lastPage is { } page ? this.reload.ExecuteAsync(page) : Task.CompletedTask;

	public async Task<bool> TryChangeSearchAsync(string? value)
	{
		if (!await this.searchDebouncer.WaitAsync() || value == Search)
		{
			return false;
		}

		Search = value;
		return true;
	}

	private static UserSort? SortOf(SortDescriptor? sort) =>
		sort?.Property switch
		{
			nameof(UserSummaryResponse.DisplayName) => UserSort.DisplayName,
			nameof(UserSummaryResponse.LastSeenAt) => UserSort.LastSeenAt,
			nameof(UserSummaryResponse.Email) => UserSort.Email,
			_ => null,
		};

	private async Task LoadAsync(LoadDataArgs args, CancellationToken token)
	{
		this.lastPage = args;
		Failure = null;

		switch (await this.api.SearchAsync(RequestOf(args), token).ToOutcome().WithBusy(this.busyState, token))
		{
			case UserPageResponse page:
				Users = page.Items;
				Total = page.Total;
				break;

			case ApiFailure failure:
				Failure = this.messages.Of(failure);
				break;
		}
	}

	private UserSearchRequest RequestOf(LoadDataArgs args)
	{
		SortDescriptor? sort = args.Sorts?.FirstOrDefault();
		UserSearchRequest request = new(Search, Descending: sort?.SortOrder == SortOrder.Descending);

		return request with { Sort = SortOf(sort) ?? request.Sort, Skip = args.Skip ?? request.Skip, Top = args.Top ?? request.Top, };
	}
}
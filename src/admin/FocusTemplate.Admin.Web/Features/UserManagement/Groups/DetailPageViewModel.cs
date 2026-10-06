using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Client.Groups;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Web.Foundation;
using FocusTemplate.Admin.Web.Foundation.Feedback;
using FocusTemplate.Admin.Web.Foundation.Pages;
using FocusTemplate.Primitives;

namespace FocusTemplate.Admin.Web.Features.UserManagement.Groups;

public sealed class DetailPageViewModel : IViewModel, IDisposable
{
	private readonly AsyncCommand<UserId> addMember;

	private readonly GroupsClient api;

	private readonly BusyState busyState;

	private readonly AsyncCommand<GroupId> delete;

	private readonly AsyncCommand<GroupId> load;

	private readonly AsyncCommand<UserId> removeMember;

	private readonly AsyncCommand<GroupId> savePermissions;

	private HashSet<Permission> granted = [];

	private GroupId id;

	public DetailPageViewModel(GroupsClient api, BusyState busyState)
	{
		this.api = api;
		this.busyState = busyState;

		this.load = new AsyncCommand<GroupId>(FetchAsync, AsyncCommandMode.ReplaceRunning);
		this.addMember = new AsyncCommand<UserId>(AddMemberAsync, AsyncCommandMode.IgnoreWhileRunning);
		this.removeMember = new AsyncCommand<UserId>(RemoveMemberAsync, AsyncCommandMode.IgnoreWhileRunning);
		this.savePermissions = new AsyncCommand<GroupId>(SavePermissionsAsync, AsyncCommandMode.IgnoreWhileRunning);
		this.delete = new AsyncCommand<GroupId>(DeleteAsync, AsyncCommandMode.IgnoreWhileRunning);
	}

	public event Action? Changed
	{
		add => this.busyState.BusyChanged += value;
		remove => this.busyState.BusyChanged -= value;
	}

	public bool Acting =>
		this.addMember.IsRunning || this.removeMember.IsRunning || this.savePermissions.IsRunning ||
		this.delete.IsRunning;

	public bool Deleted { get; private set; }

	public string? Failure { get; private set; }

	public IReadOnlySet<Permission> Granted => this.granted;

	public GroupResponse? Group { get; private set; }

	public bool Loading => this.busyState.IsBusy;

	public bool Missing { get; private set; }

	public bool PermissionsSaved { get; private set; }

	public Task AddMemberAsync(UserId user) => this.addMember.ExecuteAsync(user);

	public Task DeleteAsync() => this.delete.ExecuteAsync(this.id);

	public void Dispose()
	{
		this.load.Dispose();
		this.addMember.Dispose();
		this.removeMember.Dispose();
		this.savePermissions.Dispose();
		this.delete.Dispose();
	}

	public void Grant(Permission permission, bool on)
	{
		if (on)
		{
			this.granted.Add(permission);
		}
		else
		{
			this.granted.Remove(permission);
		}

		PermissionsSaved = false;
	}

	public Task LoadAsync(GroupId group)
	{
		this.id = group;
		return this.load.ExecuteAsync(group);
	}

	public Task RemoveMemberAsync(UserId user) => this.removeMember.ExecuteAsync(user);

	// The grants being edited stay, so a rename loses none of them
	public void Renamed(GroupResponse group) => Group = group;

	public Task SavePermissionsAsync() => this.savePermissions.ExecuteAsync(this.id);

	private async Task AddMemberAsync(UserId user, CancellationToken token)
	{
		ApiOutcome<Success> outcome = await this.api.AddMemberAsync(this.id, user, token).ToOutcome();

		Failure = outcome is ApiFailure failure ? failure.Message : null;
		if (outcome is Success)
		{
			await LoadAsync(this.id);
		}
	}

	private async Task DeleteAsync(GroupId group, CancellationToken token)
	{
		ApiOutcome<Success> outcome = await this.api.DeleteAsync(group, token).ToOutcome();

		Failure = outcome is ApiFailure failure ? failure.Message : null;
		Deleted = outcome is Success;
	}

	private async Task FetchAsync(GroupId group, CancellationToken token)
	{
		Failure = null;

		switch (await this.api.GetAsync(group, token).ToOutcome<NotFoundProblem>().WithBusy(this.busyState, token))
		{
			case GroupResponse found:
				Group = found;
				Missing = false;
				this.granted = [.. found.Permissions,];
				break;

			case NotFoundProblem:
				Group = null;
				Missing = true;
				this.granted = [];
				break;

			case ApiFailure failure:
				Failure = failure.Message;
				break;
		}
	}

	private async Task RemoveMemberAsync(UserId user, CancellationToken token)
	{
		ApiOutcome<Success> outcome = await this.api.RemoveMemberAsync(this.id, user, token).ToOutcome();

		Failure = outcome is ApiFailure failure ? failure.Message : null;
		if (outcome is Success)
		{
			await LoadAsync(this.id);
		}
	}

	private async Task SavePermissionsAsync(GroupId group, CancellationToken token)
	{
		ApiOutcome<Success> outcome = await this.api.SetPermissionsAsync(group, [.. this.granted,], token).ToOutcome();

		Failure = outcome is ApiFailure failure ? failure.Message : null;
		PermissionsSaved = outcome is Success;
	}
}
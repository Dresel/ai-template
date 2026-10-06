using System.Net;
using FocusTemplate.Admin.Client.Users;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Web.Features.UserManagement.Users;
using FocusTemplate.Admin.Web.Foundation;
using Microsoft.Extensions.Time.Testing;
using Radzen;

namespace FocusTemplate.Admin.Web.UnitTests.Features.UserManagement.Users;

public sealed class ListPageViewModelTests : IDisposable
{
	private static readonly TimeSpan SearchPause = TimeSpan.FromMilliseconds(300);

	private static readonly TimeSpan ShowBusyAfter = TimeSpan.FromMilliseconds(300);

	private static readonly TimeSpan ShowBusyAtLeast = TimeSpan.FromMilliseconds(500);

	private readonly BusyState busyState;

	private readonly FakeTimeProvider clock = new();

	private readonly TestUsersHandler handler = new();

	private readonly ListPageViewModel list;

	private readonly Debouncer searchDebouncer;

	public ListPageViewModelTests()
	{
		this.busyState = new BusyState(ShowBusyAfter, ShowBusyAtLeast, this.clock);
		this.searchDebouncer = new Debouncer(SearchPause, this.clock);
		this.list = new ListPageViewModel(
			new UsersClient(new HttpClient(this.handler) { BaseAddress = new Uri("http://localhost/"), }),
			this.busyState,
			this.searchDebouncer);
	}

	[Fact]
	public async Task ANewerPageCancelsTheOneStillLoading()
	{
		Task first = this.list.LoadAsync(new LoadDataArgs { Skip = 0, Top = 10, });
		Task second = this.list.LoadAsync(new LoadDataArgs { Skip = 10, Top = 10, });
		this.handler.Answer(1, "Second");
		await Task.WhenAll(first, second);

		Assert.True(this.handler.WasCanceled(0));
		Assert.Equal(["Second",], this.list.Users!.Select(user => user.DisplayName));
		Assert.False(this.list.Loading);
		Assert.Null(this.list.Failure);
	}

	[Fact]
	public async Task AReloadAsksForTheSamePageAgainAndTakesNoSecondClickMeanwhile()
	{
		Task load = this.list.LoadAsync(new LoadDataArgs { Skip = 10, Top = 10, });
		this.handler.Answer(0, "Before");
		await load;

		Task reload = this.list.ReloadAsync();
		bool reloadingMeanwhile = this.list.Reloading;
		Task secondClick = this.list.ReloadAsync();
		this.handler.Answer(1, "After");
		await Task.WhenAll(reload, secondClick);

		Assert.True(reloadingMeanwhile);
		Assert.False(this.list.Reloading);
		Assert.Equal([10, 10,], this.handler.Requests.Select(request => request.Skip));
		Assert.Equal(["After",], this.list.Users!.Select(user => user.DisplayName));
	}

	[Fact]
	public async Task ASlowPageShowsTheMaskUntilItsRowsArrive()
	{
		TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
		this.list.Changed += () => changed.TrySetResult();

		Task load = this.list.LoadAsync(new LoadDataArgs { Skip = 0, Top = 10, });
		this.clock.Advance(ShowBusyAfter);
		await changed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		bool maskedWhileWaiting = this.list.Loading;
		this.handler.Answer(0, "Anna");
		this.clock.Advance(ShowBusyAtLeast);
		await load;

		Assert.True(maskedWhileWaiting);
		Assert.False(this.list.Loading);
		Assert.Equal(["Anna",], this.list.Users!.Select(user => user.DisplayName));
	}

	[Fact]
	public async Task AnUnchangedSearchDoesNotSearchAgain()
	{
		Task<bool> first = this.list.TryChangeSearchAsync("dev");
		this.clock.Advance(SearchPause);
		bool changed = await first;
		Task<bool> again = this.list.TryChangeSearchAsync("dev");
		this.clock.Advance(SearchPause);

		Assert.True(changed);
		Assert.False(await again);
	}

	[Fact]
	public async Task AnUnexpectedAnswerBecomesTheFailure()
	{
		Task load = this.list.LoadAsync(new LoadDataArgs { Skip = 0, Top = 10, });
		this.handler.Fail(0, HttpStatusCode.InternalServerError);
		await load;

		Assert.Equal("The server answered 500.", this.list.Failure);
		Assert.False(this.list.Loading);
	}

	public void Dispose()
	{
		this.list.Dispose();
		this.searchDebouncer.Dispose();
		this.busyState.Dispose();
		this.handler.Dispose();
	}

	[Fact]
	public async Task OnlyAValueThatStoodForThePauseSearches()
	{
		Task<bool> first = this.list.TryChangeSearchAsync("de");
		Task<bool> second = this.list.TryChangeSearchAsync("dev");
		this.clock.Advance(SearchPause);

		Assert.False(await first);
		Assert.True(await second);
		Assert.Equal("dev", this.list.Search);
	}

	[Fact]
	public async Task TheEMailColumnSortsByTheEMailAddress()
	{
		Task load = this.list.LoadAsync(
			new LoadDataArgs
			{
				Skip = 0,
				Top = 10,
				Sorts =
				[
					new SortDescriptor
					{
						Property = nameof(UserSummaryResponse.Email), SortOrder = SortOrder.Descending,
					},
				],
			});
		this.handler.Answer(0, "Anna");
		await load;

		Assert.Equal((UserSort.Email, true), (this.handler.Requests[0].Sort, this.handler.Requests[0].Descending));
	}
}
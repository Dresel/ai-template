using FocusTemplate.Admin.Web.Foundation;
using Microsoft.Extensions.Time.Testing;

namespace FocusTemplate.Admin.Web.UnitTests.Foundation;

public sealed class BusyStateTests : IDisposable
{
	private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(300);

	private static readonly TimeSpan Minimum = TimeSpan.FromMilliseconds(500);

	private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(1);

	private readonly BusyState busy;

	private readonly TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly FakeTimeProvider clock = new();

	public BusyStateTests()
	{
		this.busy = new BusyState(Delay, Minimum, this.clock);
		this.busy.BusyChanged += () => this.changed.TrySetResult();
	}

	[Fact]
	public async Task AQuickRunShowsNoBusyState()
	{
		using CancellationTokenSource run = new();
		TaskCompletionSource<int> answer = Answer();

		Task<int> work = answer.Task.WithBusy(this.busy, run.Token);
		this.clock.Advance(Delay - Tick);
		answer.SetResult(1);
		int value = await work;
		this.clock.Advance(Delay);

		Assert.Equal(1, value);
		Assert.False(this.busy.IsBusy);
		Assert.False(this.changed.Task.IsCompleted);
	}

	[Fact]
	public async Task AReplacedRunsFailureBecomesItsCancellation()
	{
		using CancellationTokenSource run = new();
		await run.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			Task.FromException<int>(new HttpRequestException("Gone.")).WithBusy(this.busy, run.Token));
	}

	// The answer arrived, but a newer run replaced this one before it could be shown
	[Fact]
	public async Task AReplacedRunsValueBecomesItsCancellation()
	{
		using CancellationTokenSource run = new();
		await run.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			Task.FromResult(1).WithBusy(this.busy, run.Token));
	}

	[Fact]
	public async Task ARunReplacedWhileTheBusyStateHoldsEndsWithoutItsValue()
	{
		using CancellationTokenSource first = new();
		using CancellationTokenSource second = new();
		TaskCompletionSource<int> firstAnswer = Answer();
		TaskCompletionSource<int> secondAnswer = Answer();

		Task<int> firstWork = firstAnswer.Task.WithBusy(this.busy, first.Token);
		this.clock.Advance(Delay);
		await ChangedAsync();
		firstAnswer.SetResult(1);
		bool firstDoneBeforeTheMinimum = await FinishesWithoutTheClockAsync(firstWork);
		await first.CancelAsync();
		Task<int> secondWork = secondAnswer.Task.WithBusy(this.busy, second.Token);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstWork);
		bool busyWhileTheSecondRuns = this.busy.IsBusy;
		secondAnswer.SetResult(2);
		this.clock.Advance(Minimum);
		int value = await secondWork;

		Assert.False(firstDoneBeforeTheMinimum);
		Assert.True(busyWhileTheSecondRuns);
		Assert.Equal(2, value);
		Assert.False(this.busy.IsBusy);
	}

	[Fact]
	public async Task ASecondRunWhileLoadingDoesNotRestartTheDelay()
	{
		using CancellationTokenSource first = new();
		using CancellationTokenSource second = new();
		TimeSpan between = TimeSpan.FromMilliseconds(100);

		Task<int> firstWork = Pending(first.Token).WithBusy(this.busy, first.Token);
		this.clock.Advance(Delay - between);
		await first.CancelAsync();
		TaskCompletionSource<int> secondAnswer = Answer();
		Task<int> secondWork = secondAnswer.Task.WithBusy(this.busy, second.Token);
		this.clock.Advance(between);
		await ChangedAsync();
		bool busyOnceTheFirstOutlastedTheDelay = this.busy.IsBusy;
		secondAnswer.SetResult(2);
		this.clock.Advance(Minimum);
		await secondWork;

		Assert.True(busyOnceTheFirstOutlastedTheDelay);
		Assert.False(this.busy.IsBusy);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstWork);
	}

	[Fact]
	public async Task ASlowRunKeepsTheBusyStateForTheMinimum()
	{
		using CancellationTokenSource run = new();
		TaskCompletionSource<int> answer = Answer();

		Task<int> work = answer.Task.WithBusy(this.busy, run.Token);
		this.clock.Advance(Delay);
		await ChangedAsync();
		bool busyAfterTheDelay = this.busy.IsBusy;
		answer.SetResult(1);
		bool doneBeforeTheMinimum = await FinishesWithoutTheClockAsync(work);
		bool busyBeforeTheMinimum = this.busy.IsBusy;
		this.clock.Advance(Minimum);
		await work;

		Assert.True(busyAfterTheDelay);
		Assert.False(doneBeforeTheMinimum);
		Assert.True(busyBeforeTheMinimum);
		Assert.False(this.busy.IsBusy);
	}

	public void Dispose() => this.busy.Dispose();

	[Fact]
	public async Task TheCurrentRunsFailureReachesTheCallerAndEndsTheBusyState()
	{
		using CancellationTokenSource run = new();

		await Assert.ThrowsAsync<HttpRequestException>(() =>
			Task.FromException<int>(new HttpRequestException("Gone.")).WithBusy(this.busy, run.Token));

		Assert.False(this.busy.IsBusy);
	}

	private static TaskCompletionSource<int> Answer() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	// Real time for the continuations to go as far as they get without the fake clock, which only the test moves
	private static async Task<bool> FinishesWithoutTheClockAsync(Task work) =>
		await Task.WhenAny(work, Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken)) ==
		work;

	// A fetch that never answers, ending cancelled with its run as a real one does
	private static Task<int> Pending(CancellationToken token)
	{
		TaskCompletionSource<int> answer = Answer();
		token.Register(() => answer.TrySetCanceled(token));

		return answer.Task;
	}

	private Task ChangedAsync() =>
		this.changed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
}
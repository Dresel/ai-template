using FocusTemplate.Admin.Web.Foundation;

namespace FocusTemplate.Admin.Web.UnitTests.Foundation;

public sealed class AsyncCommandTests
{
	private readonly Dictionary<int, TaskCompletionSource> answers = [];
	private readonly List<(int Argument, CancellationToken Token)> runs = [];

	[Fact]
	public async Task AFailureOfTheRunReachesTheCaller()
	{
		using AsyncCommand<int> command = new(
			(_, _) => Task.FromException(new HttpRequestException("Gone.")),
			AsyncCommandMode.ReplaceRunning);

		await Assert.ThrowsAsync<HttpRequestException>(() => command.ExecuteAsync(1));

		Assert.False(command.IsRunning);
	}

	[Fact]
	public async Task IgnoringWhileRunningDropsASecondExecution()
	{
		using AsyncCommand<int> command = Command(AsyncCommandMode.IgnoreWhileRunning);

		Task first = command.ExecuteAsync(1);
		Task second = command.ExecuteAsync(2);
		this.answers[1].SetResult();
		await Task.WhenAll(first, second);

		Assert.Equal([1,], this.runs.Select(run => run.Argument));
		Assert.False(this.runs[0].Token.IsCancellationRequested);
	}

	[Fact]
	public async Task ReplacingCancelsTheRunningExecutionAndStartsTheNewOne()
	{
		using AsyncCommand<int> command = Command(AsyncCommandMode.ReplaceRunning);

		Task first = command.ExecuteAsync(1);
		Task second = command.ExecuteAsync(2);
		await first;
		bool runningAfterTheReplacedOne = command.IsRunning;
		this.answers[2].SetResult();
		await second;

		Assert.Equal([1, 2,], this.runs.Select(run => run.Argument));
		Assert.True(this.runs[0].Token.IsCancellationRequested);
		Assert.True(runningAfterTheReplacedOne);
		Assert.False(command.IsRunning);
	}

	[Fact]
	public async Task TheCommandRunsWhileItsExecutionDoes()
	{
		using AsyncCommand<int> command = Command(AsyncCommandMode.IgnoreWhileRunning);

		Task execution = command.ExecuteAsync(1);
		bool runningMeanwhile = command.IsRunning;
		this.answers[1].SetResult();
		await execution;

		Assert.True(runningMeanwhile);
		Assert.False(command.IsRunning);
	}

	// Each run waits for its answer, or ends cancelled once its token is
	private AsyncCommand<int> Command(AsyncCommandMode mode) =>
		new(
			(argument, cancellationToken) =>
			{
				TaskCompletionSource answer = new(TaskCreationOptions.RunContinuationsAsynchronously);
				cancellationToken.Register(() => answer.TrySetCanceled(cancellationToken));
				this.runs.Add((argument, cancellationToken));
				this.answers[argument] = answer;

				return answer.Task;
			},
			mode);
}
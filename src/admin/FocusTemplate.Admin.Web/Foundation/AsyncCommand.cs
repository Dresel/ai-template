namespace FocusTemplate.Admin.Web.Foundation;

// Runs on the renderer's dispatcher like the component using it, so its state needs no locks
internal sealed class AsyncCommand<T>(Func<T, CancellationToken, Task> execute, AsyncCommandMode mode) : IDisposable
{
	private CancellationTokenSource? running;

	public bool IsRunning => this.running is not null;

	public void Dispose() => this.running?.Cancel();

	// A replaced execution's cancellation concerns nobody anymore, so it ends quietly
	public async Task ExecuteAsync(T argument)
	{
		if (this.running is not null)
		{
			if (mode == AsyncCommandMode.IgnoreWhileRunning)
			{
				return;
			}

			// ReSharper disable once MethodHasAsyncOverload
			this.running.Cancel();
		}

		CancellationTokenSource current = new();
		this.running = current;

		try
		{
			await execute(argument, current.Token);
		}
		catch (OperationCanceledException) when (current.IsCancellationRequested)
		{
		}
		finally
		{
			// A replaced execution leaves running to the one replacing it
			if (ReferenceEquals(this.running, current))
			{
				this.running = null;
			}

			current.Dispose();
		}
	}
}
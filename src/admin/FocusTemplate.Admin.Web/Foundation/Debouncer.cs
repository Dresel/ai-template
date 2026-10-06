namespace FocusTemplate.Admin.Web.Foundation;

public sealed class Debouncer(TimeSpan pause, TimeProvider clock) : IDisposable
{
	private CancellationTokenSource? waiting;

	public void Dispose() => CancelAndDispose(ref this.waiting);

	// True for the call that stood for the pause, false for one a later call replaced
	public async Task<bool> WaitAsync()
	{
		CancelAndDispose(ref this.waiting);

		this.waiting = new CancellationTokenSource();
		CancellationToken token = this.waiting.Token;

		try
		{
			await Task.Delay(pause, clock, token);
			return true;
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
			return false;
		}
	}

	private static void CancelAndDispose(ref CancellationTokenSource? source)
	{
		source?.Cancel();
		source?.Dispose();
		source = null;
	}
}
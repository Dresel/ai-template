namespace FocusTemplate.Admin.Web.Foundation;

public sealed class BusyState(TimeSpan showAfter, TimeSpan showAtLeast, TimeProvider clock) : IDisposable
{
	private Task minimumShown = Task.CompletedTask;

	private CancellationTokenSource? period;

	public event Action? BusyChanged;

	public bool IsBusy { get; private set; }

	public void Dispose() => CancelAndDispose(ref this.period);

	internal void Start()
	{
		if (this.period is not null)
		{
			return;
		}

		this.period = new CancellationTokenSource();
		_ = ShowAfterDelayAsync(this.period.Token);
	}

	internal async Task StopAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Stops within the delay window, otherwise awaits minimumShown if delay window has passed
		// ReSharper disable once MethodHasAsyncOverload
		this.period?.Cancel();

		await this.minimumShown.WaitAsync(cancellationToken);

		// The minimum may have run out just as a newer run replaced this one
		cancellationToken.ThrowIfCancellationRequested();

		Hide();
	}

	private static void CancelAndDispose(ref CancellationTokenSource? source)
	{
		source?.Cancel();
		source?.Dispose();
		source = null;
	}

	private void Hide()
	{
		CancelAndDispose(ref this.period);

		this.minimumShown = Task.CompletedTask;

		if (IsBusy)
		{
			IsBusy = false;
			BusyChanged?.Invoke();
		}
	}

	private void Show()
	{
		this.minimumShown = Task.Delay(showAtLeast, clock, CancellationToken.None);

		IsBusy = true;
		BusyChanged?.Invoke();
	}

	private async Task ShowAfterDelayAsync(CancellationToken token)
	{
		try
		{
			await Task.Delay(showAfter, clock, token);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		// The delay may have ended just before the newest run stopped it
		if (token.IsCancellationRequested)
		{
			return;
		}

		Show();
	}
}
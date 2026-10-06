using FocusTemplate.Admin.Web.Foundation;
using Microsoft.Extensions.Time.Testing;

namespace FocusTemplate.Admin.Web.UnitTests.Foundation;

public sealed class DebouncerTests : IDisposable
{
	private static readonly TimeSpan Pause = TimeSpan.FromMilliseconds(300);

	private readonly FakeTimeProvider clock = new();

	private readonly Debouncer debouncer;

	public DebouncerTests()
	{
		debouncer = new Debouncer(Pause, clock);
	}

	[Fact]
	public async Task ACallStandsOnceThePausePassed()
	{
		Task<bool> call = debouncer.WaitAsync();
		clock.Advance(Pause - TimeSpan.FromMilliseconds(1));
		bool doneBeforeThePause = call.IsCompleted;
		clock.Advance(TimeSpan.FromMilliseconds(1));

		Assert.False(doneBeforeThePause);
		Assert.True(await call);
	}

	[Fact]
	public async Task ALaterCallReplacesTheOneWaiting()
	{
		Task<bool> first = debouncer.WaitAsync();
		Task<bool> second = debouncer.WaitAsync();
		clock.Advance(Pause);

		Assert.False(await first);
		Assert.True(await second);
	}

	public void Dispose() => debouncer.Dispose();

	[Fact]
	public async Task DisposingEndsTheWaitingCallAndMayHappenTwice()
	{
		Task<bool> call = debouncer.WaitAsync();
		debouncer.Dispose();
		debouncer.Dispose();

		Assert.False(await call);
	}
}
using SpatialFocus.Http;
using SpatialFocus.Weather.Client;
using SpatialFocus.Weather.Client.Stations;
using SpatialFocus.Weather.Shared;
using SpatialFocus.Web.Foundation;
using SpatialFocus.Web.Foundation.Feedback;
using SpatialFocus.Web.Foundation.Pages;

namespace SpatialFocus.Weather.Web.Stations;

public sealed class ListPageViewModel : IViewModel, IDisposable
{
	private readonly StationsClient api;

	private readonly BusyState busyState;

	private readonly AsyncCommand<ValueTuple> load;

	private readonly ApiFailureMessages messages;

	public ListPageViewModel(StationsClient api, BusyState busyState, ApiFailureMessages messages)
	{
		this.api = api;
		this.busyState = busyState;
		this.messages = messages;

		this.load = new AsyncCommand<ValueTuple>((_, cancellationToken) => FetchAsync(cancellationToken), AsyncCommandMode.ReplaceRunning);
	}

	public event Action? Changed
	{
		add => this.busyState.BusyChanged += value;
		remove => this.busyState.BusyChanged -= value;
	}

	public string? Failure { get; private set; }

	public bool Loading => this.busyState.IsBusy;

	public IReadOnlyList<StationResponse>? Stations { get; private set; }

	public void Dispose() => this.load.Dispose();

	public Task LoadAsync() => this.load.ExecuteAsync(default);

	// The dialog's answer is the station as the server stored it, so the row needs no second request
	public void Replace(StationResponse station) =>
		Stations = Stations?.Select(current => current.Id == station.Id ? station : current).ToArray();

	private async Task FetchAsync(CancellationToken cancellationToken)
	{
		switch (await this.api.ListAsync(cancellationToken).ToOutcome().WithBusy(this.busyState, cancellationToken))
		{
			case IReadOnlyList<StationResponse> stations:
				Stations = stations;
				break;

			case ApiFailure failure:
				Failure = this.messages.Of(failure);
				break;
		}
	}
}
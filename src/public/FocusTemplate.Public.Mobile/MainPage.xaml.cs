using FocusTemplate.Public.Client.WeatherForecasts;
using FocusTemplate.Public.Shared;

namespace FocusTemplate.Public.Mobile;

public partial class MainPage : ContentPage
{
	private readonly WeatherForecastsClient weatherForecasts;

	public MainPage(WeatherForecastsClient weatherForecasts)
	{
		this.weatherForecasts = weatherForecasts;
		InitializeComponent();
	}

	private async void OnLoadWeatherClicked(object? sender, EventArgs e)
	{
		try
		{
			IReadOnlyList<WeatherForecastResponse> forecast = await this.weatherForecasts.ListAsync();
			this.WeatherList.ItemsSource = forecast;
		}
		catch (Exception exception)
		{
			await DisplayAlertAsync("Weather", exception.Message, "OK");
		}
	}
}
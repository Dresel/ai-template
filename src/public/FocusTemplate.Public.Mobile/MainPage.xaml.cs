using FocusTemplate.Public.Shared;

namespace FocusTemplate.Public.Mobile;

public partial class MainPage : ContentPage
{
	private readonly WeatherApiClient weatherApiClient;

	public MainPage(WeatherApiClient weatherApiClient)
	{
		this.weatherApiClient = weatherApiClient;
		InitializeComponent();
	}

	private async void OnLoadWeatherClicked(object? sender, EventArgs e)
	{
		try
		{
			WeatherForecastResponse[] forecast = await this.weatherApiClient.GetWeatherAsync();
			this.WeatherList.ItemsSource = forecast;
		}
		catch (Exception exception)
		{
			await DisplayAlertAsync("Weather", exception.Message, "OK");
		}
	}
}
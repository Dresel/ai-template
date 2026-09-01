using Microsoft.Extensions.Hosting;

namespace FocusTemplate.Public.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		MauiAppBuilder builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Aspire client defaults: service discovery, resilience, OpenTelemetry.
		builder.AddServiceDefaults();

		// "https+http://" prefers HTTPS and falls back to HTTP - "public-api" resolves via service discovery
		// (the AppHost injects the endpoint, via Dev Tunnel on Android/iOS).
		builder.Services.AddHttpClient<WeatherApiClient>(client => client.BaseAddress = new Uri("https+http://public-api"));

		builder.Services.AddSingleton<MainPage>();

		return builder.Build();
	}
}
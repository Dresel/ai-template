using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Support.UI;

namespace FocusTemplate.Public.Mobile.E2E;

[Collection(AppiumCollection.Name)]
public sealed class WeatherSmokeTests(AppiumFixture fixture)
{
	[Fact]
	public void LoadWeatherShowsSeededForecasts()
	{
		AppiumDriver driver = fixture.EnsureReady();
		WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
		wait.IgnoreExceptionTypes(typeof(WebDriverException));

		// MAUI AutomationId = Android resource-id, so MobileBy.Id (not AccessibilityId). The driver adds the package prefix.
		wait.Message = "LoadWeatherButton did not appear";
		AppiumElement button = wait.Until(_ => driver.FindElements(MobileBy.Id("LoadWeatherButton")).FirstOrDefault());
		button.Click();

		// CollectionView items carry no AutomationIds of their own. The temperature label
		// ("18 °C", ...) is the most robust per-row marker.
		wait.Message = "WeatherList did not populate";
		List<AppiumElement> temperatureLabels = wait.Until(_ =>
		{
			List<AppiumElement> labels =
			[
				.. driver.FindElements(MobileBy.ClassName("android.widget.TextView"))
					.Where(label => label.Text.Contains("°C", StringComparison.Ordinal)),
			];
			return labels.Count > 0 ? labels : null;
		})!;

		// WeatherSeed inserts exactly five forecasts through the production seeding path.
		Assert.Equal(5, temperatureLabels.Count);
	}
}
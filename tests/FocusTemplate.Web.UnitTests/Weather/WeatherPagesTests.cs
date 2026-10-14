using FocusTemplate.Admin.Web.Foundation.Pages;
using FocusTemplate.Weather.Web;
using Microsoft.Extensions.DependencyInjection;
using WeatherPages = FocusTemplate.Weather.Web.ServiceCollectionExtensions;

namespace FocusTemplate.Web.UnitTests.Weather;

public sealed class WeatherPagesTests
{
	[Fact]
	public void TheFeatureHandsItsPagesToTheHostsRouter()
	{
		ServiceCollection services = new();
		services.AddWeatherPages(new TestHostEnvironment(), "admin-api");
		using ServiceProvider provider = services.BuildServiceProvider();

		PageAssembly pages = Assert.Single(provider.GetServices<PageAssembly>());
		Assert.Equal(typeof(WeatherPages).Assembly, pages.Assembly);
	}
}
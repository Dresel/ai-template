using Microsoft.Extensions.DependencyInjection;
using SpatialFocus.Weather.Web;
using SpatialFocus.Web.Foundation.Pages;
using WeatherPages = SpatialFocus.Weather.Web.ServiceCollectionExtensions;

namespace SpatialFocus.Web.UnitTests.Weather;

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
using FocusTemplate.Admin.Web.Foundation.Pages;
using FocusTemplate.Weather.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeatherPages = FocusTemplate.Weather.Web.ServiceCollectionExtensions;

namespace FocusTemplate.Admin.Web.UnitTests.Features.Weather;

public sealed class WeatherPagesTests
{
	[Fact]
	public void TheFeatureHandsItsPagesToTheHostsRouter()
	{
		ServiceCollection services = new();
		services.AddWeatherPages(new HostEnvironment(), "admin-api");
		using ServiceProvider provider = services.BuildServiceProvider();

		PageAssembly pages = Assert.Single(provider.GetServices<PageAssembly>());
		Assert.Equal(typeof(WeatherPages).Assembly, pages.Assembly);
	}

	private sealed class HostEnvironment : IWebAssemblyHostEnvironment
	{
		public string BaseAddress => "http://localhost/";

		public string Environment => Environments.Development;
	}
}
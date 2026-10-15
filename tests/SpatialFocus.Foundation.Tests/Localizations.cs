using Microsoft.Extensions.DependencyInjection;
using SpatialFocus.Localization;
using SpatialFocus.Web.Foundation;

namespace SpatialFocus.Foundation.Tests;

// Foundation's texts as a host registers them, for the classes a test builds by hand
internal static class Localizations
{
	private static readonly ServiceProvider Services = new ServiceCollection()
		.AddLogging()
		.AddResxLocalization()
		.AddFoundationLocalization()
		.BuildServiceProvider();

	public static T Get<T>()
		where T : notnull =>
		Services.GetRequiredService<T>();
}
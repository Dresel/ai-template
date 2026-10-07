using FocusTemplate.Admin.Web.Infrastructure.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTemplate.Admin.Web.UnitTests;

// The texts as the Web app registers them, for the classes a test builds by hand
internal static class Localizations
{
	private static readonly ServiceProvider Services = new ServiceCollection().AddLogging().AddWebLocalization().BuildServiceProvider();

	public static T Get<T>()
		where T : notnull =>
		Services.GetRequiredService<T>();
}
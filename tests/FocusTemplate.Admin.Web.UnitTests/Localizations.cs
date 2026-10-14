using FocusTemplate.Admin.UserManagement.Web;
using FocusTemplate.Admin.Web.Infrastructure.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTemplate.Admin.Web.UnitTests;

// The texts as the Web app registers them, for the classes a test builds by hand. User management's pages register
// their own, as every Admin host includes them
internal static class Localizations
{
	private static readonly ServiceProvider Services = new ServiceCollection()
		.AddLogging()
		.AddWebLocalization()
		.AddUserManagementPages(new TestHostEnvironment(), "admin-api")
		.BuildServiceProvider();

	public static T Get<T>()
		where T : notnull =>
		Services.GetRequiredService<T>();
}
using Microsoft.Extensions.DependencyInjection;
using SpatialFocus.Admin.UserManagement.Web;
using SpatialFocus.Localization;
using SpatialFocus.Web.Foundation;

namespace SpatialFocus.Admin.UserManagement.Tests;

// The texts as a host registers them, for the classes a test builds by hand: Foundation's, and user management's, which
// its pages register
internal static class Localizations
{
	private static readonly ServiceProvider Services = new ServiceCollection()
		.AddLogging()
		.AddResxLocalization()
		.AddFoundationLocalization()
		.AddUserManagementPages(new TestHostEnvironment(), "admin-api", [])
		.BuildServiceProvider();

	public static T Get<T>()
		where T : notnull =>
		Services.GetRequiredService<T>();
}
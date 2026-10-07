using System.Globalization;

namespace FocusTemplate.Admin.Web.UnitTests;

// Sets texts and formats to one culture for a test, as the app does, and puts the ones before back, so no test
// depends on the machine's language
internal sealed class UiCulture : IDisposable
{
	private readonly CultureInfo previous = CultureInfo.CurrentCulture;

	private readonly CultureInfo previousUi = CultureInfo.CurrentUICulture;

	private UiCulture(string name)
	{
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
		CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
	}

	public static UiCulture Use(string name) => new(name);

	public void Dispose()
	{
		CultureInfo.CurrentCulture = this.previous;
		CultureInfo.CurrentUICulture = this.previousUi;
	}
}
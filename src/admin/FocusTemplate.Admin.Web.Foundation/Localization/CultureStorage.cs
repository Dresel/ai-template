using System.Globalization;
using Microsoft.JSInterop;

namespace FocusTemplate.Admin.Web.Foundation.Localization;

// The choice lives in culture.js, which the page's head loads. Without a choice, the browser's language if the app
// speaks it.
public sealed class CultureStorage(IJSRuntime js, SupportedCultures cultures)
{
	public async Task ChooseAsync(CultureInfo culture)
	{
		try
		{
			await js.InvokeVoidAsync("culture.choose", culture.Name);
		}
		catch (JSException)
		{
			// A page without the script keeps no choice, and the reload brings back its language
		}
	}

	public async Task<CultureInfo> ReadAsync()
	{
		string? stored;
		try
		{
			stored = await js.InvokeAsync<string?>("culture.read");
		}
		catch (JSException)
		{
			stored = null;
		}

		return cultures.Resolve(stored is null ? CultureInfo.CurrentUICulture : Parse(stored));
	}

	private CultureInfo Parse(string name)
	{
		try
		{
			return CultureInfo.GetCultureInfo(name);
		}
		catch (CultureNotFoundException)
		{
			return cultures.Default;
		}
	}
}
using System.Globalization;
using FocusTemplate.Admin.Shared.Localization;
using Microsoft.JSInterop;

namespace FocusTemplate.Admin.Web.Infrastructure.Localization;

// The language a user chose, kept in the browser. Without a choice, the browser's language if the app speaks it.
public sealed class CultureStorage(IJSRuntime js)
{
	private const string Key = "culture";

	public async Task<CultureInfo> ReadAsync()
	{
		string? stored;
		try
		{
			stored = await js.InvokeAsync<string?>("localStorage.getItem", Key);
		}
		catch (JSException)
		{
			// Some browsers refuse local storage in a private window
			stored = null;
		}

		return Cultures.ResolveOrDefault(stored is null ? CultureInfo.CurrentUICulture : Parse(stored));
	}

	public async Task WriteAsync(CultureInfo culture)
	{
		try
		{
			await js.InvokeVoidAsync("localStorage.setItem", Key, culture.Name);
		}
		catch (JSException)
		{
			// Without local storage the choice is lost with the reload, and the page stays in its language
		}
	}

	private static CultureInfo Parse(string name)
	{
		try
		{
			return CultureInfo.GetCultureInfo(name);
		}
		catch (CultureNotFoundException)
		{
			return Cultures.Default;
		}
	}
}
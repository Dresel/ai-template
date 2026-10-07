using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

namespace FocusTemplate.Admin.Web.Infrastructure.Localization;

public static class WebAssemblyHostExtensions
{
	// Before RunAsync, which loads the satellite assemblies of the culture set by then
	public static async Task UseStoredCultureAsync(this WebAssemblyHost host)
	{
		CultureInfo culture = await host.Services.GetRequiredService<CultureStorage>().ReadAsync();

		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;

		try
		{
			// For the browser: screen readers, hyphenation and spell checking follow it
			await host.Services.GetRequiredService<IJSRuntime>()
				.InvokeVoidAsync("document.documentElement.setAttribute", "lang", culture.Name);
		}
		catch (JSException)
		{
			// The app works in the culture anyway, only the page's lang attribute keeps its default
		}
	}
}
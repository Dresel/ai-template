using Microsoft.JSInterop;

namespace SpatialFocus.Web.Foundation.Appearance;

// The choice lives in appearance.js, which the page's head loads, so the theme is set before .NET runs. A page without
// the script falls back to the light theme and keeps no choice.
public sealed class AppearanceStorage(IJSRuntime js, AppearanceThemes themes)
{
	// In process, so a component knows the theme before its first render
	private IJSInProcessRuntime InProcess => (IJSInProcessRuntime)js;

	public async Task<string> ChooseAsync(AppearanceMode mode)
	{
		try
		{
			return ThemeOf(await js.InvokeAsync<AppearanceMode>("appearance.choose", mode));
		}
		catch (JSException)
		{
			return ThemeOf(mode);
		}
	}

	public AppearanceMode ReadMode()
	{
		try
		{
			return InProcess.Invoke<AppearanceMode>("appearance.mode");
		}
		catch (JSException)
		{
			return AppearanceMode.System;
		}
	}

	public string ReadTheme()
	{
		try
		{
			return ThemeOf(InProcess.Invoke<AppearanceMode>("appearance.theme"));
		}
		catch (JSException)
		{
			return themes.Light;
		}
	}

	public string ThemeOf(AppearanceMode resolved) => resolved == AppearanceMode.Dark ? themes.Dark : themes.Light;

	public async ValueTask WatchAsync<T>(DotNetObjectReference<T> reference)
		where T : class
	{
		try
		{
			await js.InvokeVoidAsync("appearance.watch", reference);
		}
		catch (JSException)
		{
			// The theme then stays as it is when the system's scheme changes
		}
	}
}
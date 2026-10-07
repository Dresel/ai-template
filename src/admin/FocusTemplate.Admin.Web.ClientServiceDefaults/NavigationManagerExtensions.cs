using System.Globalization;
using Microsoft.AspNetCore.Components;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

public static class NavigationManagerExtensions
{
	// A full load, since the BFF's login lives outside the client router. The culture goes along for Keycloak's form.
	public static void NavigateToLogin(this NavigationManager navigation) =>
		navigation.NavigateTo(
			$"bff/login?returnUrl={Uri.EscapeDataString($"/{navigation.ToBaseRelativePath(navigation.Uri)}")}&culture={Uri.EscapeDataString(CultureInfo.CurrentUICulture.Name)}",
			true);
}
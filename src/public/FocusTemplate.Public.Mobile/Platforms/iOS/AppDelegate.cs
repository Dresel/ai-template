using Foundation;

// Platform bootstrap files conventionally keep the app root namespace.
#pragma warning disable IDE0130
namespace FocusTemplate.Public.Mobile;
#pragma warning restore IDE0130

[Register("AppDelegate")]
#pragma warning disable CA1711 // "AppDelegate" is the canonical iOS application delegate name
public class AppDelegate : MauiUIApplicationDelegate
#pragma warning restore CA1711
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
using Android.App;
using Android.Content.PM;

// Platform bootstrap files conventionally keep the app root namespace.
#pragma warning disable IDE0130
namespace FocusTemplate.Public.Mobile;
#pragma warning restore IDE0130

[Activity(
	Theme = "@style/Maui.SplashTheme",
	MainLauncher = true,
	LaunchMode = LaunchMode.SingleTop,
	ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
		ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
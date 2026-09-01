using Android.App;
using Android.Runtime;

// Platform bootstrap files conventionally keep the app root namespace.
#pragma warning disable IDE0130
namespace FocusTemplate.Public.Mobile;
#pragma warning restore IDE0130

[Application]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
using UIKit;

// Platform bootstrap files conventionally keep the app root namespace.
#pragma warning disable IDE0130
namespace FocusTemplate.Public.Mobile;
#pragma warning restore IDE0130

public class Program
{
	private static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}
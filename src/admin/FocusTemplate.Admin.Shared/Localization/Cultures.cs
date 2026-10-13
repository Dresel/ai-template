using System.Globalization;

namespace FocusTemplate.Admin.Shared.Localization;

// The languages of the UI and of the API's messages. A fork with another default changes it here.
public static class Cultures
{
	public static readonly CultureInfo Default = CultureInfo.GetCultureInfo("en");

	public static readonly IReadOnlyList<CultureInfo> Supported = [Default, CultureInfo.GetCultureInfo("de"),];

	public static readonly HashSet<string> SupportedLanguages =
	[
		with(StringComparer.OrdinalIgnoreCase), .. Supported.Select(culture => culture.TwoLetterISOLanguageName),
	];

	public static bool IsSupported(CultureInfo culture) => SupportedLanguages.Contains(culture.TwoLetterISOLanguageName);
}
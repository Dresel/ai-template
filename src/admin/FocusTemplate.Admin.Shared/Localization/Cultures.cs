using System.Globalization;

namespace FocusTemplate.Admin.Shared.Localization;

// The languages of the UI and of the API's messages. A fork with another default changes it here.
public static class Cultures
{
	public static readonly CultureInfo Default = CultureInfo.GetCultureInfo("en");

	public static readonly IReadOnlyList<CultureInfo> Supported = [Default, CultureInfo.GetCultureInfo("de"),];

	public static readonly HashSet<string> SupportedLanguages =
		[with(StringComparer.OrdinalIgnoreCase), .. Supported.Select(culture => culture.TwoLetterISOLanguageName),];

	public static bool IsSupported(CultureInfo culture) => SupportedLanguages.Contains(culture.TwoLetterISOLanguageName);

	// A supported culture stays as it is, so de-AT keeps its date and number formats.
	public static CultureInfo ResolveOrDefault(CultureInfo culture) => IsSupported(culture) ? culture : Default;
}
using System.Globalization;

namespace SpatialFocus.Web.Foundation.Localization;

// The cultures the host speaks, its default first. A culture counts as supported when its language is.
public sealed class SupportedCultures(IReadOnlyList<CultureInfo> all)
{
	private readonly string[] languages = [.. all.Select(culture => culture.TwoLetterISOLanguageName),];

	public CultureInfo Default => all[0];

	public CultureInfo Next(CultureInfo current) => all[(IndexOf(current) + 1) % all.Count];

	// A supported culture stays as it is, so de-AT keeps its date and number formats
	public CultureInfo Resolve(CultureInfo culture) => IndexOf(culture) < 0 ? Default : culture;

	private int IndexOf(CultureInfo culture) => Array.IndexOf(languages, culture.TwoLetterISOLanguageName);
}
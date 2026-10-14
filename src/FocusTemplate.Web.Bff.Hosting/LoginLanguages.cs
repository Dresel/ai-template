using System.Globalization;

namespace FocusTemplate.Web.Bff.Hosting;

// The languages the app speaks, which alone reach Keycloak's form, as their two-letter tag
internal sealed class LoginLanguages(IReadOnlyList<CultureInfo> supported)
{
	private readonly HashSet<string> languages =
		[with(StringComparer.OrdinalIgnoreCase), .. supported.Select(culture => culture.TwoLetterISOLanguageName),];

	public string? Of(string? culture)
	{
		try
		{
			return culture is null || CultureInfo.GetCultureInfo(culture) is not { } parsed ||
				!this.languages.Contains(parsed.TwoLetterISOLanguageName)
					? null
					: parsed.TwoLetterISOLanguageName;
		}
		catch (CultureNotFoundException)
		{
			return null;
		}
	}
}
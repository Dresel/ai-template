using FocusTemplate.Admin.Web.Features.UserManagement.Users;

namespace FocusTemplate.Admin.Web.UnitTests.Infrastructure.Localization;

public sealed class FormattedTextsTests
{
	[Theory]
	[InlineData("de", "Zuerst gesehen 07.10.2026 14:30, zuletzt gesehen 07.10.2026 14:30")]
	[InlineData("en-GB", "First seen 07/10/2026 14:30, last seen 07/10/2026 14:30")]
	public void AValueInATextIsFormattedInTheLanguageOfTheUi(string culture, string expected)
	{
		using UiCulture ui = UiCulture.Use(culture);
		DateTimeOffset seen = new(2026, 10, 7, 14, 30, 0, TimeSpan.Zero);

		Assert.Equal(expected, Localizations.Get<IDetailPageLocalizations>().Seen(seen, seen));
	}
}
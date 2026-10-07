using System.Globalization;
using FocusTemplate.Admin.Shared.Localization;

namespace FocusTemplate.Admin.Web.UnitTests.Infrastructure.Localization;

public sealed class CulturesTests
{
	[Theory]
	[InlineData("de-AT", "de-AT")]
	[InlineData("de", "de")]
	[InlineData("en-GB", "en-GB")]
	[InlineData("fr-FR", "en")]
	public void ASupportedLanguageKeepsItsRegionAndAnotherBecomesEnglish(string browser, string resolved) =>
		Assert.Equal(resolved, Cultures.ResolveOrDefault(CultureInfo.GetCultureInfo(browser)).Name);
}
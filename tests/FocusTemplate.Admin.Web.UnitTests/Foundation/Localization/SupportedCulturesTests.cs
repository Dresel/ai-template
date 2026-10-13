using System.Globalization;
using FocusTemplate.Admin.Shared.Localization;
using FocusTemplate.Admin.Web.Foundation.Localization;

namespace FocusTemplate.Admin.Web.UnitTests.Foundation.Localization;

public sealed class SupportedCulturesTests
{
	private readonly SupportedCultures cultures = new(Cultures.Supported);

	[Theory]
	[InlineData("de-AT", "de-AT")]
	[InlineData("de", "de")]
	[InlineData("en-GB", "en-GB")]
	[InlineData("fr-FR", "en")]
	public void ASupportedLanguageKeepsItsRegionAndAnotherBecomesEnglish(string browser, string resolved) =>
		Assert.Equal(resolved, this.cultures.Resolve(CultureInfo.GetCultureInfo(browser)).Name);

	[Theory]
	[InlineData("en", "de")]
	[InlineData("en-GB", "de")]
	[InlineData("de-AT", "en")]
	[InlineData("fr-FR", "en")]
	public void TheNextLanguageFollowsInTheListAndTheLastLeadsBackToTheFirst(string current, string next) =>
		Assert.Equal(next, this.cultures.Next(CultureInfo.GetCultureInfo(current)).Name);
}
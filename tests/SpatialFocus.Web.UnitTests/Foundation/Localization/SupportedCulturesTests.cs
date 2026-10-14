using System.Globalization;
using SpatialFocus.Web.Foundation.Localization;

namespace SpatialFocus.Web.UnitTests.Foundation.Localization;

public sealed class SupportedCulturesTests
{
	// A host's list, its first language the default
	private readonly SupportedCultures cultures = new([CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("de"),]);

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
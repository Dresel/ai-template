using FocusTemplate.Admin.Shared.Localization;
using FocusTemplate.Http;
using FocusTemplate.Weather.Shared;

namespace FocusTemplate.Admin.Web.UnitTests.Features.Weather;

// A feature's contract texts sit beside its contracts, in its own assembly. The hook every service shares finds them,
// and answers null where no resx names the member.
public sealed class StationTextsTests
{
	[Fact]
	public void AFeaturesContractNamesComeFromTheResxBesideItsContracts()
	{
		ValidationTexts.UseResources();
		using UiCulture german = UiCulture.Use("de");

		Assert.Equal(
			"Name",
			ContractTexts.DisplayName?.Invoke(typeof(StationRenameRequest), nameof(StationRenameRequest.Name)));
	}
}
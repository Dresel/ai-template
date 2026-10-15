using SpatialFocus.Http;
using SpatialFocus.Localization;
using SpatialFocus.Weather.Shared;

namespace SpatialFocus.Weather.Tests;

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
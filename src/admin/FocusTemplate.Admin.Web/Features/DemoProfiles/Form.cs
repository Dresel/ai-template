namespace FocusTemplate.Admin.Web.Features.DemoProfiles;

internal sealed class Form
{
	public AddressForm Address { get; } = new();

	public int? Age { get; set; }

	// The demo's switch: off, only the server validates
	public bool ClientRules { get; set; } = true;

	public string? Code { get; set; }

	public string? DisplayName { get; set; }

	public int? MaxTemperatureC { get; set; }

	public int? MinTemperatureC { get; set; }

	public string? Nickname { get; set; }

	public List<TagRow> Tags { get; } = [];

	public double? WeightKg { get; set; }
}
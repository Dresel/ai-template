namespace FocusTemplate.Admin.Web.UnitTests.Foundation.Validation.Core;

public sealed class TestForm
{
	public TestAddress Address { get; } = new();

	public string? DisplayName { get; set; }

	public int? Max { get; set; }

	public int? Min { get; set; }

	public string? Name { get; set; }

	public string? Nickname { get; set; }

	public List<TestTag> Tags { get; } = [];
}
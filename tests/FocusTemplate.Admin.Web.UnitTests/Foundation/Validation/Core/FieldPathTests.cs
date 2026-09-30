using FocusTemplate.Admin.Web.Foundation.Validation.Core;

namespace FocusTemplate.Admin.Web.UnitTests.Foundation.Validation.Core;

public sealed class FieldPathTests
{
	private readonly TestForm form = new();

	[Fact]
	public void AListRowsPathStartsAtTheRow()
	{
		TestTag tag = new();

		Assert.Equal("Value", FieldPath.Of(() => tag.Value));
	}

	[Fact]
	public void ALocalFormStartsTheSameWay()
	{
		TestForm local = new();

		Assert.Equal("Name", FieldPath.Of(() => local.Name));
	}

	[Fact]
	public void AMemberChainSpellsThePathAfterTheFormItStartsFrom() =>
		Assert.Equal("Address.Street", FieldPath.Of(() => this.form.Address.Street));

	[Fact]
	public void AValidatorsExpressionStartsAfterItsModel() =>
		Assert.Equal("Address.Street", FieldPath.Of((TestForm model) => model.Address.Street));

	[Fact]
	public void PathsOverlapWhenTheyNameTheSameFieldOrOneLiesWithinTheOther()
	{
		Assert.True(FieldPath.Overlap("Address", "Address.Street"));
		Assert.True(FieldPath.Overlap("Tags[1]", "Tags"));
		Assert.True(FieldPath.Overlap("Name", "Name"));
		Assert.False(FieldPath.Overlap("Name", "Nickname"));
		Assert.False(FieldPath.Overlap("Address.Street", "Address.City"));
	}
}
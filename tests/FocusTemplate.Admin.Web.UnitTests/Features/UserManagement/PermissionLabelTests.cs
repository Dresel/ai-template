using FocusTemplate.Admin.Web.Features.UserManagement;
using FocusTemplate.Primitives;

namespace FocusTemplate.Admin.Web.UnitTests.Features.UserManagement;

public sealed class PermissionLabelTests
{
	[Fact]
	public void APermissionReadsAsItsSectionAndItsAction() =>
		Assert.Equal(
			new PermissionLabel("User management", "Manage groups"),
			PermissionLabel.Of(UserManagementPermissions.ManageGroups));

	[Fact]
	public void AnAcronymStaysOneWordInCapitals() =>
		Assert.Equal(
			new PermissionLabel("Integrations", "Rotate API keys"),
			PermissionLabel.Of(Permission.From("Integrations.RotateAPIKeys")));
}
using FocusTemplate.Admin.Web.Features.UserManagement;
using FocusTemplate.Primitives;
using Microsoft.Extensions.Localization;

namespace FocusTemplate.Admin.Web.UnitTests.Features.UserManagement;

public sealed class PermissionLabelsTests
{
	private static readonly PermissionLabels Labels = new(Localizations.Get<IStringLocalizer<PermissionLabels>>());

	[Fact]
	public void APermissionReadsAsItsSectionAndItsAction()
	{
		using UiCulture english = UiCulture.Use("en");

		Assert.Equal(new PermissionLabel("User management", "Manage groups"), Labels.Of(UserManagementPermissions.ManageGroups));
	}

	[Fact]
	public void APermissionTheResxDoesNotKnowReadsOffItsNameInGermanToo()
	{
		using UiCulture german = UiCulture.Use("de");

		Assert.Equal(new PermissionLabel("Integrations", "Rotate API keys"), Labels.Of(Permission.From("Integrations.RotateAPIKeys")));
	}

	[Fact]
	public void AnAcronymStaysOneWordInCapitals()
	{
		using UiCulture english = UiCulture.Use("en");

		Assert.Equal(new PermissionLabel("Integrations", "Rotate API keys"), Labels.Of(Permission.From("Integrations.RotateAPIKeys")));
	}

	[Fact]
	public void InGermanAPermissionReadsAsTheResxSays()
	{
		using UiCulture german = UiCulture.Use("de");

		Assert.Equal(new PermissionLabel("Benutzerverwaltung", "Gruppen verwalten"), Labels.Of(UserManagementPermissions.ManageGroups));
	}
}
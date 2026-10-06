using FocusTemplate.Admin.Web.Infrastructure.Authorization;
using FocusTemplate.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FocusTemplate.Admin.Web.UnitTests.Infrastructure.Authorization;

// AuthorizeView takes only a policy or roles, so a part of a page asks for a permission through PermissionView, the same
// requirement a page's attribute yields
public sealed class PermissionViewTests
{
	[Fact]
	public async Task APartOfAPageIsHiddenFromAUserWithoutItsPermission()
	{
		string html = await RenderAsync(UserManagementPermissions.ManageGroups, UserManagementPermissions.Names.ViewGroups);

		Assert.Contains("not authorized", html, StringComparison.Ordinal);
		Assert.DoesNotContain("the guarded part", html, StringComparison.Ordinal);
	}

	[Fact]
	public async Task APartOfAPageShowsToAUserHoldingItsPermission()
	{
		string html = await RenderAsync(UserManagementPermissions.ManageGroups, UserManagementPermissions.Names.ManageGroups);

		Assert.Contains("the guarded part", html, StringComparison.Ordinal);
	}

	private static Task<string> RenderAsync(Permission required, params string[] permissions) =>
		AuthorizationRenderer.RenderAsync<PermissionView>(
			new Dictionary<string, object?>
			{
				[nameof(PermissionView.Permission)] = required,
				[nameof(PermissionView.Authorized)] =
					(RenderFragment<AuthenticationState>)(_ =>
						builder => builder.AddContent(0, "the guarded part")),
				[nameof(PermissionView.NotAuthorized)] =
					(RenderFragment<AuthenticationState>)(_ => builder => builder.AddContent(0, "not authorized")),
			},
			permissions);
}
using FocusTemplate.Admin.Shared;
using FocusTemplate.Primitives.Permissions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;

namespace FocusTemplate.Admin.Web.UnitTests.Infrastructure.Authorization;

// AuthorizeRouteView honors IAuthorizationRequirementData from .NET 11, so a page carries the attribute the generated endpoints carry, and no policy is registered
public sealed class RequiresPermissionTests
{
	[Fact]
	public async Task APageRequiringAPermissionIsRefusedToAUserWithoutIt()
	{
		string html = await RenderAsync(UserManagement.Names.ViewGroups);

		Assert.Contains("not authorized", html, StringComparison.Ordinal);
		Assert.DoesNotContain("the protected page", html, StringComparison.Ordinal);
	}

	[Fact]
	public async Task APageRequiringAPermissionShowsToAUserHoldingIt()
	{
		string html = await RenderAsync(UserManagement.Names.ViewUsers);

		Assert.Contains("the protected page", html, StringComparison.Ordinal);
	}

	private static Task<string> RenderAsync(params string[] permissions) =>
		AuthorizationRenderer.RenderAsync<AuthorizeRouteView>(
			new Dictionary<string, object?>
			{
				[nameof(AuthorizeRouteView.RouteData)] =
					new RouteData(typeof(ProtectedPage), new Dictionary<string, object?>()),
				[nameof(AuthorizeRouteView.NotAuthorized)] =
					(RenderFragment<AuthenticationState>)(_ => builder => builder.AddContent(0, "not authorized")),
			},
			permissions);

	[RequiresPermission(UserManagement.Names.ViewUsers)]
	private sealed class ProtectedPage : ComponentBase
	{
		protected override void BuildRenderTree(RenderTreeBuilder builder) =>
			builder.AddContent(0, "the protected page");
	}
}
using System.Security.Claims;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Web.Infrastructure.Authorization;
using FocusTemplate.Primitives.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

	private static async Task<string> RenderAsync(params string[] permissions)
	{
		ClaimsPrincipal user = new(
			new ClaimsIdentity(
				[.. permissions.Select(permission => new Claim(BffClaimTypes.Permission, permission)),],
				"test"));

		ServiceCollection services = new();
		services.AddLogging();
		services.AddAuthorizationCore();
		services.AddCascadingAuthenticationState();
		services.AddSingleton<AuthenticationStateProvider>(new FixedAuthenticationStateProvider(user));
		services.AddSingleton<IAuthorizationHandler, PermissionClaimsHandler>();

		await using ServiceProvider provider = services.BuildServiceProvider();
		await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());

		return await renderer.Dispatcher.InvokeAsync(async () =>
		{
			ParameterView parameters = ParameterView.FromDictionary(
				new Dictionary<string, object?>
				{
					[nameof(AuthorizeRouteView.RouteData)] =
						new RouteData(typeof(ProtectedPage), new Dictionary<string, object?>()),
					[nameof(AuthorizeRouteView.NotAuthorized)] =
						(RenderFragment<AuthenticationState>)(_ =>
							builder => builder.AddContent(0, "not authorized")),
				});

			return (await renderer.RenderComponentAsync<AuthorizeRouteView>(parameters)).ToHtmlString();
		});
	}

	private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
	{
		public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
			Task.FromResult(new AuthenticationState(user));
	}

	[RequiresPermission(UserManagement.Names.ViewUsers)]
	private sealed class ProtectedPage : ComponentBase
	{
		protected override void BuildRenderTree(RenderTreeBuilder builder) =>
			builder.AddContent(0, "the protected page");
	}
}
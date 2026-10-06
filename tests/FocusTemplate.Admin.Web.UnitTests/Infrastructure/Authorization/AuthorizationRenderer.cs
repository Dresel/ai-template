using System.Security.Claims;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FocusTemplate.Admin.Web.UnitTests.Infrastructure.Authorization;

// With the authorization the client registers, for a signed-in user holding the permissions of these names
internal static class AuthorizationRenderer
{
	public static async Task<string> RenderAsync<TComponent>(
		IDictionary<string, object?> parameters,
		params string[] permissions)
		where TComponent : IComponent
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
			(await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters))).ToHtmlString());
	}

	private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
	{
		public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
			Task.FromResult(new AuthenticationState(user));
	}
}
using System.Security.Claims;
using FocusTemplate.Admin.Web.Infrastructure.Authorization;
using FocusTemplate.Primitives;
using Microsoft.AspNetCore.Components.Authorization;

namespace FocusTemplate.Admin.Web.Infrastructure.Authentication;

// Reads the state without awaiting it, since a page behind [Authorize] renders only once the router has it
public sealed class CurrentUser : IDisposable
{
	private readonly AuthenticationStateProvider authentication;

	private Task<AuthenticationState> state;

	public CurrentUser(AuthenticationStateProvider authentication)
	{
		this.authentication = authentication;
		this.state = authentication.GetAuthenticationStateAsync();

		authentication.AuthenticationStateChanged += Changed;
	}

	public UserId? Id => Guid.TryParse(Principal.FindFirst("sub")?.Value, out Guid id) ? UserId.From(id) : null;

	private ClaimsPrincipal Principal =>
		this.state.IsCompletedSuccessfully
			? this.state.Result.User
			: throw new InvalidOperationException("The authentication state is read before the router knows it.");

	public void Dispose() => this.authentication.AuthenticationStateChanged -= Changed;

	public bool Has(Permission permission) => Principal.Has(permission);

	private void Changed(Task<AuthenticationState> changed) => this.state = changed;
}
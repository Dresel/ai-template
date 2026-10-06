using FocusTemplate.Admin.Shared;
using FocusTemplate.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FocusTemplate.Admin.Web.Infrastructure.Authorization;

// AuthorizeView takes only a policy or roles, so this asks for the requirement a page's RequiresPermission yields
public sealed class PermissionView : AuthorizeViewCore
{
	[Parameter]
	[EditorRequired]
	public Permission Permission { get; set; }

	protected override IAuthorizeData[] GetAuthorizeData() => [new PermissionData(Permission),];

	// AuthorizeViewCore takes IAuthorizeData only, and CombineAsync reads the requirement off the same object
	private sealed class PermissionData(Permission permission) : IAuthorizeData, IAuthorizationRequirementData
	{
		public string? AuthenticationSchemes { get; set; }

		public string? Policy { get; set; }

		public string? Roles { get; set; }

		public IEnumerable<IAuthorizationRequirement> GetRequirements() => [new PermissionRequirement(permission),];
	}
}
namespace FocusTemplate.Admin.Api.Features.DemoProfiles;

public static partial class DemoProfilesEndpoints
{
	static partial void ConfigureGroup(RouteGroupBuilder group) => group.RequireAuthorization();
}
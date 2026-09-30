namespace FocusTemplate.Admin.Api.Features.DemoProfiles;

public sealed class DemoProfileCodes
{
	public Task<bool> IsTakenAsync(string code) => Task.FromResult(code == "TAK");
}
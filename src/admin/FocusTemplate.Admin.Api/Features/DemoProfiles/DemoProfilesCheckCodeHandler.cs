using FocusTemplate.Admin.Shared;
using Mediator;

namespace FocusTemplate.Admin.Api.Features.DemoProfiles;

public sealed class DemoProfilesCheckCodeHandler(DemoProfileCodes codes)
	: IQueryHandler<DemoProfilesCheckCodeQuery, DemoProfilesCheckCodeResult>
{
	public async ValueTask<DemoProfilesCheckCodeResult> Handle(
		DemoProfilesCheckCodeQuery query,
		CancellationToken cancellationToken) =>
		new DemoCodeResponse(query.Code, await codes.IsTakenAsync(query.Code));
}
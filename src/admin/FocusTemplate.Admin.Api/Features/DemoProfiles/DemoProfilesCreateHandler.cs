using FluentValidation.Results;
using FocusTemplate.Admin.Shared;
using Mediator;

namespace FocusTemplate.Admin.Api.Features.DemoProfiles;

public sealed class DemoProfilesCreateHandler(IDemoProfilesLocalizations localizations)
	: ICommandHandler<DemoProfilesCreateCommand, DemoProfilesCreateResult>
{
	public ValueTask<DemoProfilesCreateResult> Handle(DemoProfilesCreateCommand command, CancellationToken cancellationToken) =>
		ValueTask.FromResult<DemoProfilesCreateResult>(
			string.Equals(command.Body.Name, "Reserved", StringComparison.OrdinalIgnoreCase)
				? new ValidationProblem(ValidationProblems.Of(new ValidationResult([ReservedName(),])))
				: new DemoProfileResponse(command.Body.Code, command.Body.Name));

	// Found after the validators passed, the way a lookup would find it, and answered as a value
	private ValidationFailure ReservedName() =>
		new(AdminPaths.DemoProfileRequest.Name, localizations.NameReserved) { ErrorCode = "name.reserved", };
}